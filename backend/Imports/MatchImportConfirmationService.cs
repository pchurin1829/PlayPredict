using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Imports;

public sealed class MatchImportConfirmationService
{
    private static readonly ImportConfirmationSummary EmptySummary = new(0, 0, 0);
    private readonly PlayPredictDbContext db;
    private readonly SpreadsheetReader reader;
    private readonly MatchImportPreviewService previewService;
    private readonly ILogger<MatchImportConfirmationService> logger;
    private readonly TimeProvider timeProvider;

    public MatchImportConfirmationService(
        PlayPredictDbContext db,
        ILogger<MatchImportConfirmationService> logger,
        TimeProvider? timeProvider = null,
        SpreadsheetReader? reader = null,
        MatchImportPreviewService? previewService = null)
    {
        this.db = db;
        this.logger = logger;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.reader = reader ?? new SpreadsheetReader();
        this.previewService = previewService ?? new MatchImportPreviewService(db);
    }

    public async Task<MatchImportConfirmationResult> ConfirmAsync(
        Stream file,
        string fileName,
        int editionId,
        int adminUserId,
        string expectedSha256,
        CancellationToken cancellationToken = default)
    {
        var (content, actualHash) = await SpreadsheetFileHash.ReadAndComputeSha256Async(file, cancellationToken);
        if (!string.Equals(actualHash, SpreadsheetTextNormalizer.Clean(expectedSha256), StringComparison.OrdinalIgnoreCase))
            return Rejected(actualHash, "El archivo no coincide con el analizado previamente.",
                [new("FILE_HASH_MISMATCH", "La huella SHA-256 del archivo no coincide con la esperada.")]);

        using var spreadsheetStream = new MemoryStream(content, writable: false);
        var spreadsheet = reader.Read(spreadsheetStream, fileName, SpreadsheetImportKind.Matches);
        if (!spreadsheet.IsValid)
            return Rejected(actualHash, "El archivo contiene errores estructurales.", spreadsheet.Issues);

        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var preview = await previewService.PreviewAsync(spreadsheet, editionId, cancellationToken);
            if (!preview.CanConfirm)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Rejected(actualHash, "La importación fue rechazada durante la revalidación.", CollectProblems(preview));
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;

            // Crea, una sola vez por Order distinto, cada Fecha que el preview detectó como faltante.
            var roundsByOrder = new Dictionary<int, Round>();
            foreach (var order in preview.Matches.Where(row => row.RoundIsNew).Select(row => row.RoundOrder!.Value).Distinct())
            {
                var round = new Round { EditionId = editionId, Name = $"Fecha {order}", Order = order };
                db.Rounds.Add(round);
                roundsByOrder[order] = round;
            }
            if (roundsByOrder.Count > 0) await db.SaveChangesAsync(cancellationToken);

            var created = 0;
            var updated = 0;
            foreach (var row in preview.Matches)
            {
                if (row.Classification == MatchImportClassification.MatchCreate)
                {
                    var roundId = row.RoundId ?? roundsByOrder[row.RoundOrder!.Value].Id;
                    var homeTeam = await db.Teams.SingleAsync(team => team.Id == row.HomeTeamId!.Value, cancellationToken);
                    var awayTeam = await db.Teams.SingleAsync(team => team.Id == row.AwayTeamId!.Value, cancellationToken);
                    db.Matches.Add(new Match
                    {
                        RoundId = roundId,
                        HomeTeamId = homeTeam.Id,
                        AwayTeamId = awayTeam.Id,
                        ParticipantHome = homeTeam.Name,
                        ParticipantAway = awayTeam.Name,
                        StartsAtUtc = row.StartsAtUtc!.Value,
                        Status = Enum.Parse<MatchStatus>(row.Status!),
                        CreatedAtUtc = now
                    });
                    created++;
                }
                else if (row.Classification == MatchImportClassification.MatchUpdate)
                {
                    var match = await db.Matches.SingleAsync(m => m.Id == row.MatchId!.Value, cancellationToken);
                    match.StartsAtUtc = row.StartsAtUtc!.Value;
                    match.Status = Enum.Parse<MatchStatus>(row.Status!);
                    updated++;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var unchanged = preview.Summary.Unchanged;
            logger.LogInformation(
                "Match import confirmed. AdminUserId={AdminUserId} EditionId={EditionId} FileHash={FileHash} Created={Created} Updated={Updated} Unchanged={Unchanged}",
                adminUserId, editionId, actualHash, created, updated, unchanged);

            return new(
                ImportConfirmationStatus.Success,
                actualHash,
                "Importación confirmada correctamente.",
                new(created, updated, unchanged),
                []);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (transaction is not null)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); }
                catch { /* Preserve the original persistence error. */ }
            }
            db.ChangeTracker.Clear();
            return new(
                ImportConfirmationStatus.Failed,
                actualHash,
                "La importación falló y fue revertida por completo.",
                EmptySummary,
                [new("IMPORT_FAILED", exception.Message)]);
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private static IReadOnlyList<SpreadsheetValidationIssue> CollectProblems(MatchImportPreviewResult preview)
    {
        var issues = preview.Issues.ToList();
        issues.AddRange(preview.Matches
            .Where(row => row.Classification is MatchImportClassification.StructuralError
                or MatchImportClassification.UnresolvedTeamError
                or MatchImportClassification.DuplicateMatchRowError
                or MatchImportClassification.MatchFinishedConflict
                or MatchImportClassification.MatchTeamChangeConflict
                or MatchImportClassification.MatchRoundChangeConflict)
            .Select(row => new SpreadsheetValidationIssue(row.Classification.ToString(), row.Message, row.Sheet, row.RowNumber)));
        return issues;
    }

    private static MatchImportConfirmationResult Rejected(
        string hash,
        string message,
        IReadOnlyList<SpreadsheetValidationIssue> issues) =>
        new(ImportConfirmationStatus.Rejected, hash, message, EmptySummary, issues);
}

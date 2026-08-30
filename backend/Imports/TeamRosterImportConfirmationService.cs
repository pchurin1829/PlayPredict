using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Imports;

public sealed class TeamRosterImportConfirmationService
{
    private static readonly ImportConfirmationSummary EmptySummary = new(0, 0, 0);
    private readonly PlayPredictDbContext db;
    private readonly SpreadsheetReader reader;
    private readonly TeamRosterImportPreviewService previewService;

    public TeamRosterImportConfirmationService(
        PlayPredictDbContext db,
        SpreadsheetReader? reader = null,
        TeamRosterImportPreviewService? previewService = null)
    {
        this.db = db;
        this.reader = reader ?? new SpreadsheetReader();
        this.previewService = previewService ?? new TeamRosterImportPreviewService(db);
    }

    public async Task<TeamRosterImportConfirmationResult> ConfirmAsync(
        Stream file,
        string fileName,
        string sport,
        string expectedSha256,
        CancellationToken cancellationToken = default)
    {
        var (content, actualHash) = await SpreadsheetFileHash.ReadAndComputeSha256Async(file, cancellationToken);
        if (!string.Equals(actualHash, SpreadsheetTextNormalizer.Clean(expectedSha256), StringComparison.OrdinalIgnoreCase))
            return Rejected(actualHash, "El archivo no coincide con el analizado previamente.",
                [new("FILE_HASH_MISMATCH", "La huella SHA-256 del archivo no coincide con la esperada.")]);

        using var spreadsheetStream = new MemoryStream(content, writable: false);
        var spreadsheet = reader.Read(spreadsheetStream, fileName, SpreadsheetImportKind.TeamsAndRosters);
        if (!spreadsheet.IsValid)
            return Rejected(actualHash, "El archivo contiene errores estructurales.", spreadsheet.Issues);

        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var preview = await previewService.PreviewAsync(spreadsheet, sport, cancellationToken);
            if (!preview.CanConfirm)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Rejected(actualHash, "La importaci\u00f3n fue rechazada durante la revalidaci\u00f3n.", CollectProblems(preview));
            }

            var teamRows = spreadsheet.Teams.ToDictionary(row => row.RowNumber);
            var createdTeamsByName = new Dictionary<string, Team>(StringComparer.Ordinal);
            var teamUpdated = 0;
            foreach (var item in preview.Teams)
            {
                var source = teamRows[item.RowNumber];
                switch (item.Classification)
                {
                    case ImportPreviewClassification.TeamNew:
                        var created = new Team
                        {
                            Name = source.Name,
                            ShortName = source.ShortName,
                            Sport = preview.Sport,
                            Active = true
                        };
                        db.Teams.Add(created);
                        createdTeamsByName.Add(source.NormalizedName, created);
                        break;
                    case ImportPreviewClassification.TeamUpdatable:
                        var existing = await db.Teams.SingleAsync(team => team.Id == item.TeamId, cancellationToken);
                        existing.ShortName = source.ShortName;
                        teamUpdated++;
                        break;
                }
            }

            // Required to materialize database-generated Team IDs before creating their players.
            await db.SaveChangesAsync(cancellationToken);

            var rosterRows = spreadsheet.Rosters.ToDictionary(row => row.RowNumber);
            var playerUpdated = 0;
            foreach (var item in preview.Rosters)
            {
                var source = rosterRows[item.RowNumber];
                switch (item.Classification)
                {
                    case ImportPreviewClassification.PlayerNew:
                        var teamId = item.TeamId ?? createdTeamsByName[source.NormalizedClubName].Id;
                        db.TeamPlayers.Add(new TeamPlayer
                        {
                            TeamId = teamId,
                            FirstName = source.FirstName,
                            LastName = source.LastName,
                            DisplayName = source.DisplayName,
                            Position = item.Position,
                            Active = true
                        });
                        break;
                    case ImportPreviewClassification.PlayerUpdatable:
                        var existing = await db.TeamPlayers.SingleAsync(player => player.Id == item.TeamPlayerId, cancellationToken);
                        existing.Position = item.Position;
                        existing.DisplayName = source.DisplayName;
                        playerUpdated++;
                        break;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new(
                ImportConfirmationStatus.Success,
                actualHash,
                "Importaci\u00f3n confirmada correctamente.",
                new(preview.TeamsSummary.New, teamUpdated, preview.TeamsSummary.Unchanged),
                new(preview.RostersSummary.New, playerUpdated, preview.RostersSummary.Unchanged),
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
                "La importaci\u00f3n fall\u00f3 y fue revertida por completo.",
                EmptySummary,
                EmptySummary,
                [new("IMPORT_FAILED", exception.Message)]);
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private static IReadOnlyList<SpreadsheetValidationIssue> CollectProblems(TeamRosterImportPreviewResult preview)
    {
        var issues = preview.Issues.ToList();
        issues.AddRange(preview.Teams
            .Where(row => row.Classification is ImportPreviewClassification.StructuralError
                or ImportPreviewClassification.TeamSportConflict
                or ImportPreviewClassification.TeamAmbiguousConflict)
            .Select(row => new SpreadsheetValidationIssue(row.Classification.ToString(), row.Message, row.Sheet, row.RowNumber)));
        issues.AddRange(preview.Rosters
            .Where(row => row.Classification is ImportPreviewClassification.StructuralError
                or ImportPreviewClassification.PlayerAmbiguousConflict
                or ImportPreviewClassification.UnresolvedTeamError)
            .Select(row => new SpreadsheetValidationIssue(row.Classification.ToString(), row.Message, row.Sheet, row.RowNumber)));
        return issues;
    }

    private static TeamRosterImportConfirmationResult Rejected(
        string hash,
        string message,
        IReadOnlyList<SpreadsheetValidationIssue> issues) =>
        new(ImportConfirmationStatus.Rejected, hash, message, EmptySummary, EmptySummary, issues);
}

using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Imports;

public sealed class MatchImportPreviewService(PlayPredictDbContext db)
{
    private static readonly IReadOnlyList<ImportProposedChange> NoChanges = [];

    public async Task<MatchImportPreviewResult> PreviewAsync(
        SpreadsheetReadResult spreadsheet,
        int editionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spreadsheet);
        var issues = spreadsheet.Issues.ToList();

        var editionExists = await db.Editions.AsNoTracking().AnyAsync(e => e.Id == editionId, cancellationToken);
        if (!editionExists)
        {
            issues.Add(new("EDITION_NOT_FOUND", "La Edición seleccionada no existe."));
            return new(editionId, Summarize([]), [], issues);
        }

        var rounds = await db.Rounds.AsNoTracking().Where(r => r.EditionId == editionId).ToListAsync(cancellationToken);
        var roundsByOrder = rounds.ToDictionary(r => r.Order);
        var roundIds = rounds.Select(r => r.Id).ToHashSet();

        var teams = await db.Teams.AsNoTracking().ToListAsync(cancellationToken);
        var teamsByName = teams
            .GroupBy(team => SpreadsheetTextNormalizer.Normalize(team.Name), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        var existingMatches = await db.Matches.AsNoTracking()
            .Where(m => roundIds.Contains(m.RoundId))
            .Select(m => new MatchSnapshot(
                m.Id, m.RoundId, m.HomeTeamId, m.AwayTeamId, m.StartsAtUtc, m.Status,
                db.Predictions.Any(p => p.MatchId == m.Id)))
            .ToListAsync(cancellationToken);

        var matchesByKey = existingMatches.ToDictionary(m => (m.RoundId, m.HomeTeamId, m.AwayTeamId));
        var matchesByRound = existingMatches.ToLookup(m => m.RoundId);
        var matchesByTeamPair = existingMatches.ToLookup(m => (m.HomeTeamId, m.AwayTeamId));

        var rows = new List<MatchImportPreviewRow>(spreadsheet.Matches.Count);
        var seenKeysInFile = new HashSet<(int RoundOrder, int Home, int Away)>();

        foreach (var row in spreadsheet.Matches)
        {
            if (HasRowError(issues, row.RowNumber))
            {
                rows.Add(Row(row, MatchImportClassification.StructuralError,
                    "La fila tiene errores estructurales y no puede resolverse.", null, false, null, null, null, null));
                continue;
            }

            var roundOrder = row.RoundNumber!.Value;
            roundsByOrder.TryGetValue(roundOrder, out var round);
            var roundId = round?.Id;
            var roundName = round?.Name ?? $"Fecha {roundOrder}";
            var roundIsNew = round is null;

            var homeResolved = TryResolveTeam(row.NormalizedHomeTeam, teamsByName, out var homeTeam, out var homeAmbiguous);
            var awayResolved = TryResolveTeam(row.NormalizedAwayTeam, teamsByName, out var awayTeam, out var awayAmbiguous);

            if (!homeResolved || !awayResolved)
            {
                var message = homeAmbiguous || awayAmbiguous
                    ? "Hay múltiples equipos con el mismo nombre normalizado."
                    : "No se pudo resolver LOCAL o VISITANTE: el equipo no existe.";
                rows.Add(Row(row, MatchImportClassification.UnresolvedTeamError, message,
                    roundOrder, roundIsNew, roundId, roundName, homeTeam?.Id, awayTeam?.Id));
                continue;
            }

            if (homeTeam!.Id == awayTeam!.Id)
            {
                rows.Add(Row(row, MatchImportClassification.UnresolvedTeamError,
                    "LOCAL y VISITANTE no pueden ser el mismo equipo.",
                    roundOrder, roundIsNew, roundId, roundName, homeTeam.Id, awayTeam.Id));
                continue;
            }

            var fileKey = (roundOrder, homeTeam.Id, awayTeam.Id);
            if (!seenKeysInFile.Add(fileKey))
            {
                rows.Add(Row(row, MatchImportClassification.DuplicateMatchRowError,
                    "El partido aparece más de una vez en el archivo.",
                    roundOrder, roundIsNew, roundId, roundName, homeTeam.Id, awayTeam.Id));
                continue;
            }

            var startsAtUtc = ToArgentinaUtc(row.Date!.Value, row.Time!.Value);
            var status = MapStatus(row.Status!.Value);

            MatchSnapshot? exact = roundId.HasValue && matchesByKey.TryGetValue((roundId.Value, homeTeam.Id, awayTeam.Id), out var found)
                ? found
                : null;

            if (exact is not null)
            {
                if (exact.Status == MatchStatus.Finished)
                {
                    rows.Add(Row(row, MatchImportClassification.MatchFinishedConflict,
                        "El partido ya está Finalizado. Los resultados se administran mediante Corregir resultado, no por esta importación.",
                        roundOrder, roundIsNew, roundId, roundName, homeTeam.Id, awayTeam.Id,
                        startsAtUtc, status, exact.Id));
                    continue;
                }

                var changes = new List<ImportProposedChange>();
                if (exact.StartsAtUtc != startsAtUtc)
                    changes.Add(new("StartsAtUtc", exact.StartsAtUtc.ToString("O"), startsAtUtc.ToString("O")));
                if (exact.Status != status)
                    changes.Add(new("Status", exact.Status.ToString(), status.ToString()));

                rows.Add(Row(row,
                    changes.Count == 0 ? MatchImportClassification.MatchUnchanged : MatchImportClassification.MatchUpdate,
                    changes.Count == 0 ? "El partido existente no requiere cambios." : "El partido tiene cambios importables.",
                    roundOrder, roundIsNew, roundId, roundName, homeTeam.Id, awayTeam.Id,
                    startsAtUtc, status, exact.Id, changes));
                continue;
            }

            // No existe un partido exacto en este Round. Antes de crear, verificar que ninguno
            // de los dos equipos ya tenga un partido asignado en este mismo Round (evitaría que
            // un equipo juegue dos veces la misma Fecha) y que el mismo enfrentamiento no exista
            // ya en otro Round de la Edición (posible reprogramación ambigua: ver informe).
            if (roundId.HasValue)
            {
                var teamBusyInRound = matchesByRound[roundId.Value]
                    .FirstOrDefault(m => m.HomeTeamId == homeTeam.Id || m.AwayTeamId == homeTeam.Id
                        || m.HomeTeamId == awayTeam.Id || m.AwayTeamId == awayTeam.Id);
                if (teamBusyInRound is not null)
                {
                    var withPredictions = teamBusyInRound.HasPredictions ? " Ese partido ya tiene pronósticos cargados." : "";
                    rows.Add(Row(row, MatchImportClassification.MatchTeamChangeConflict,
                        $"LOCAL o VISITANTE ya participa en otro partido (Id {teamBusyInRound.Id}) de esta Fecha. " +
                        $"La importación nunca reasigna equipos de un partido existente.{withPredictions}",
                        roundOrder, roundIsNew, roundId, roundName, homeTeam.Id, awayTeam.Id,
                        startsAtUtc, status, null));
                    continue;
                }
            }

            var sameFixtureElsewhere = matchesByTeamPair[(homeTeam.Id, awayTeam.Id)]
                .FirstOrDefault(m => !roundId.HasValue || m.RoundId != roundId.Value);
            if (sameFixtureElsewhere is not null)
            {
                rows.Add(Row(row, MatchImportClassification.MatchRoundChangeConflict,
                    $"Ya existe un partido (Id {sameFixtureElsewhere.Id}) entre estos mismos equipos en otra Fecha de la Edición. " +
                    "La importación nunca mueve un partido de Fecha automáticamente: revisá manualmente si es una reprogramación " +
                    "o un segundo enfrentamiento legítimo.",
                    roundOrder, roundIsNew, roundId, roundName, homeTeam.Id, awayTeam.Id,
                    startsAtUtc, status, null));
                continue;
            }

            rows.Add(Row(row, MatchImportClassification.MatchCreate,
                roundIsNew ? $"Se creará la Fecha {roundOrder} y el partido." : "El partido se crearía en una confirmación posterior.",
                roundOrder, roundIsNew, roundId, roundName, homeTeam.Id, awayTeam.Id, startsAtUtc, status, null));
        }

        return new(editionId, Summarize(rows), rows, issues);
    }

    private static bool TryResolveTeam(
        string normalizedName, IReadOnlyDictionary<string, List<Team>> teamsByName, out Team? team, out bool ambiguous)
    {
        ambiguous = false;
        team = null;
        if (!teamsByName.TryGetValue(normalizedName, out var matches) || matches.Count == 0) return false;
        if (matches.Count > 1) { ambiguous = true; return false; }
        team = matches[0];
        return true;
    }

    private static DateTime ToArgentinaUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time);
        var offset = new DateTimeOffset(local, TimeSpan.FromHours(-3));
        return offset.UtcDateTime;
    }

    private static MatchStatus MapStatus(ImportMatchStatus status) => status switch
    {
        ImportMatchStatus.Scheduled => MatchStatus.Scheduled,
        ImportMatchStatus.InProgress => MatchStatus.InProgress,
        ImportMatchStatus.Suspended => MatchStatus.Suspended,
        ImportMatchStatus.Cancelled => MatchStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static bool HasRowError(IReadOnlyList<SpreadsheetValidationIssue> issues, int row) =>
        issues.Any(issue => issue.RowNumber == row && string.Equals(issue.SheetName, SpreadsheetReader.MatchesSheet, StringComparison.OrdinalIgnoreCase));

    private static MatchImportPreviewRow Row(
        ImportMatchRow row, MatchImportClassification classification, string message,
        int? roundOrder, bool roundIsNew, int? roundId, string? roundName, int? homeTeamId, int? awayTeamId,
        DateTime? startsAtUtc = null, MatchStatus? status = null, int? matchId = null,
        IReadOnlyList<ImportProposedChange>? changes = null) =>
        new(SpreadsheetReader.MatchesSheet, row.RowNumber, $"{row.HomeTeam} vs {row.AwayTeam}", classification, message,
            roundOrder, roundName, roundId, roundIsNew, row.HomeTeam, row.AwayTeam, homeTeamId, awayTeamId,
            startsAtUtc, status?.ToString(), matchId, changes ?? NoChanges);

    private static MatchImportPreviewSummary Summarize(IReadOnlyList<MatchImportPreviewRow> rows) => new(
        rows.Count,
        rows.Count(row => row.Classification == MatchImportClassification.MatchCreate),
        rows.Count(row => row.Classification == MatchImportClassification.MatchUpdate),
        rows.Count(row => row.Classification == MatchImportClassification.MatchUnchanged),
        rows.Count(row => row.Classification is MatchImportClassification.MatchFinishedConflict
            or MatchImportClassification.MatchTeamChangeConflict or MatchImportClassification.MatchRoundChangeConflict),
        rows.Count(row => row.Classification is MatchImportClassification.UnresolvedTeamError
            or MatchImportClassification.DuplicateMatchRowError or MatchImportClassification.StructuralError));

    private sealed record MatchSnapshot(int Id, int RoundId, int HomeTeamId, int AwayTeamId, DateTime StartsAtUtc, MatchStatus Status, bool HasPredictions);
}

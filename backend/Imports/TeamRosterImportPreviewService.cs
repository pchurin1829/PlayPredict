using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Imports;

public sealed class TeamRosterImportPreviewService
{
    private static readonly IReadOnlyList<ImportProposedChange> NoChanges = [];
    private readonly PlayPredictDbContext db;

    public TeamRosterImportPreviewService(PlayPredictDbContext db)
    {
        this.db = db;
    }

    public async Task<TeamRosterImportPreviewResult> PreviewAsync(
        SpreadsheetReadResult spreadsheet,
        string sport,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spreadsheet);
        var originalSport = sport ?? string.Empty;
        var cleanSport = SpreadsheetTextNormalizer.Clean(originalSport);
        var normalizedSport = SpreadsheetTextNormalizer.Normalize(cleanSport);
        var issues = spreadsheet.Issues.ToList();
        if (normalizedSport.Length == 0)
            issues.Add(new("SPORT_REQUIRED", "Debe seleccionar un deporte antes de generar el preview."));

        var databaseTeams = await db.Teams.AsNoTracking().ToListAsync(cancellationToken);
        var teamsByName = databaseTeams
            .GroupBy(team => SpreadsheetTextNormalizer.Normalize(team.Name), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        var teamRows = ClassifyTeams(spreadsheet.Teams, teamsByName, originalSport, cleanSport, normalizedSport, issues);
        var resolvedImports = BuildImportedTeamResolution(spreadsheet.Teams, teamRows);

        var databasePlayers = await db.TeamPlayers.AsNoTracking().ToListAsync(cancellationToken);
        var playersByIdentity = databasePlayers
            .GroupBy(player => PlayerKey(player.TeamId, player.FirstName, player.LastName))
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var rosterRows = ClassifyRosters(spreadsheet.Rosters, teamsByName, resolvedImports, playersByIdentity, issues, normalizedSport);

        return new(
            originalSport,
            cleanSport,
            normalizedSport,
            SummarizeTeams(teamRows),
            SummarizeRosters(rosterRows),
            teamRows,
            rosterRows,
            issues);
    }

    private static List<TeamImportPreviewRow> ClassifyTeams(
        IReadOnlyList<ImportTeamRow> source,
        IReadOnlyDictionary<string, List<Team>> teamsByName,
        string originalSport,
        string sport,
        string normalizedSport,
        IReadOnlyList<SpreadsheetValidationIssue> issues)
    {
        var result = new List<TeamImportPreviewRow>(source.Count);
        foreach (var row in source)
        {
            if (HasRowError(issues, SpreadsheetReader.TeamsSheet, row.RowNumber) || normalizedSport.Length == 0)
            {
                result.Add(TeamRow(row, ImportPreviewClassification.StructuralError,
                    "La fila tiene errores estructurales y no puede resolverse.", originalSport));
                continue;
            }

            teamsByName.TryGetValue(row.NormalizedName, out var matches);
            if (matches is null || matches.Count == 0)
            {
                result.Add(TeamRow(row, ImportPreviewClassification.TeamNew,
                    "El equipo se crear\u00eda en una confirmaci\u00f3n posterior.", sport));
                continue;
            }
            if (matches.Count > 1)
            {
                result.Add(TeamRow(row, ImportPreviewClassification.TeamAmbiguousConflict,
                    "Hay m\u00faltiples equipos con el mismo nombre normalizado.", sport));
                continue;
            }

            var team = matches[0];
            if (!SpreadsheetTextNormalizer.EqualsNormalized(team.Sport, normalizedSport))
            {
                result.Add(TeamRow(row, ImportPreviewClassification.TeamSportConflict,
                    "El deporte del equipo existente no coincide con el contexto seleccionado.", sport, team.Id));
                continue;
            }

            if (string.Equals(SpreadsheetTextNormalizer.Clean(team.ShortName), row.ShortName, StringComparison.Ordinal))
            {
                result.Add(TeamRow(row, ImportPreviewClassification.TeamUnchanged,
                    "El equipo existente no requiere cambios.", sport, team.Id));
                continue;
            }

            result.Add(TeamRow(row, ImportPreviewClassification.TeamUpdatable,
                "El nombre corto podr\u00eda actualizarse al confirmar.", sport, team.Id,
                [new("ShortName", team.ShortName, row.ShortName)]));
        }
        return result;
    }

    private static Dictionary<string, TeamImportResolution> BuildImportedTeamResolution(
        IReadOnlyList<ImportTeamRow> source,
        IReadOnlyList<TeamImportPreviewRow> previews)
    {
        var result = new Dictionary<string, TeamImportResolution>(StringComparer.Ordinal);
        for (var index = 0; index < source.Count; index++)
        {
            var preview = previews[index];
            if (preview.Classification is ImportPreviewClassification.TeamNew
                or ImportPreviewClassification.TeamUnchanged
                or ImportPreviewClassification.TeamUpdatable)
                result.TryAdd(source[index].NormalizedName, new(preview.TeamId, preview.Classification == ImportPreviewClassification.TeamNew));
        }
        return result;
    }

    private static List<RosterImportPreviewRow> ClassifyRosters(
        IReadOnlyList<ImportRosterRow> source,
        IReadOnlyDictionary<string, List<Team>> teamsByName,
        IReadOnlyDictionary<string, TeamImportResolution> importedTeams,
        IReadOnlyDictionary<string, List<TeamPlayer>> playersByIdentity,
        IReadOnlyList<SpreadsheetValidationIssue> issues,
        string normalizedSport)
    {
        var result = new List<RosterImportPreviewRow>(source.Count);
        foreach (var row in source)
        {
            if (HasRowError(issues, SpreadsheetReader.RostersSheet, row.RowNumber) || normalizedSport.Length == 0)
            {
                result.Add(RosterRow(row, ImportPreviewClassification.StructuralError,
                    "La fila tiene errores estructurales y no puede resolverse."));
                continue;
            }

            if (!TryResolveTeam(row.NormalizedClubName, teamsByName, importedTeams, normalizedSport, out var team))
            {
                result.Add(RosterRow(row, ImportPreviewClassification.UnresolvedTeamError,
                    "No se pudo resolver el equipo del jugador de forma segura."));
                continue;
            }

            var desiredPosition = PositionLabel(row.Position);
            if (team.IsNew)
            {
                result.Add(RosterRow(row, ImportPreviewClassification.PlayerNew,
                    "El jugador se crear\u00eda junto con el equipo nuevo.", null, null, NoChanges, desiredPosition));
                continue;
            }

            playersByIdentity.TryGetValue(PlayerKey(team.TeamId!.Value, row.FirstName, row.LastName), out var matches);
            if (matches is null || matches.Count == 0)
            {
                result.Add(RosterRow(row, ImportPreviewClassification.PlayerNew,
                    "El jugador se crear\u00eda en el equipo existente.", team.TeamId, null, NoChanges, desiredPosition));
                continue;
            }
            if (matches.Count > 1)
            {
                result.Add(RosterRow(row, ImportPreviewClassification.PlayerAmbiguousConflict,
                    "Hay m\u00faltiples jugadores compatibles en el equipo.", team.TeamId, null, NoChanges, desiredPosition));
                continue;
            }

            var player = matches[0];
            var changes = new List<ImportProposedChange>();
            if (!SpreadsheetTextNormalizer.EqualsNormalized(player.Position, desiredPosition))
                changes.Add(new("Position", player.Position, desiredPosition));
            if (!string.Equals(SpreadsheetTextNormalizer.Clean(player.DisplayName), row.DisplayName, StringComparison.Ordinal))
                changes.Add(new("DisplayName", player.DisplayName, row.DisplayName));

            result.Add(RosterRow(row,
                changes.Count == 0 ? ImportPreviewClassification.PlayerUnchanged : ImportPreviewClassification.PlayerUpdatable,
                changes.Count == 0 ? "El jugador existente no requiere cambios." : "El jugador tiene cambios importables.",
                team.TeamId, player.Id, changes, desiredPosition));
        }
        return result;
    }

    private static bool TryResolveTeam(
        string normalizedName,
        IReadOnlyDictionary<string, List<Team>> databaseTeams,
        IReadOnlyDictionary<string, TeamImportResolution> importedTeams,
        string normalizedSport,
        out TeamImportResolution resolution)
    {
        if (importedTeams.TryGetValue(normalizedName, out resolution!)) return true;
        if (!databaseTeams.TryGetValue(normalizedName, out var matches) || matches.Count != 1)
        {
            resolution = default!;
            return false;
        }
        var team = matches[0];
        if (!SpreadsheetTextNormalizer.EqualsNormalized(team.Sport, normalizedSport))
        {
            resolution = default!;
            return false;
        }
        resolution = new(team.Id, false);
        return true;
    }

    private static bool HasRowError(IReadOnlyList<SpreadsheetValidationIssue> issues, string sheet, int row) =>
        issues.Any(issue => issue.RowNumber == row && string.Equals(issue.SheetName, sheet, StringComparison.OrdinalIgnoreCase));

    private static string PlayerKey(int teamId, string firstName, string lastName) =>
        $"{teamId}|{SpreadsheetTextNormalizer.Normalize(firstName)}|{SpreadsheetTextNormalizer.Normalize(lastName)}";

    private static string? PositionLabel(ImportPlayerPosition? position) => position switch
    {
        ImportPlayerPosition.Goalkeeper => PlayerPositionCatalog.Labels[PlayerPosition.Goalkeeper],
        ImportPlayerPosition.Defender => PlayerPositionCatalog.Labels[PlayerPosition.Defender],
        ImportPlayerPosition.Midfielder => PlayerPositionCatalog.Labels[PlayerPosition.Midfielder],
        ImportPlayerPosition.Forward => PlayerPositionCatalog.Labels[PlayerPosition.Forward],
        _ => null
    };

    private static TeamImportPreviewRow TeamRow(ImportTeamRow row, ImportPreviewClassification classification,
        string message, string sport, int? teamId = null, IReadOnlyList<ImportProposedChange>? changes = null) =>
        new(SpreadsheetReader.TeamsSheet, row.RowNumber, row.Name, classification, message,
            row.Name, row.ShortName, sport, teamId, changes ?? NoChanges);

    private static RosterImportPreviewRow RosterRow(ImportRosterRow row, ImportPreviewClassification classification,
        string message, int? teamId = null, int? playerId = null, IReadOnlyList<ImportProposedChange>? changes = null,
        string? position = null) =>
        new(SpreadsheetReader.RostersSheet, row.RowNumber, row.DisplayName, classification, message,
            row.ClubName, row.FirstName, row.LastName, row.DisplayName, position, teamId, playerId, changes ?? NoChanges);

    private static TeamImportPreviewSummary SummarizeTeams(IReadOnlyList<TeamImportPreviewRow> rows) => new(
        rows.Count,
        rows.Count(row => row.Classification == ImportPreviewClassification.TeamNew),
        rows.Count(row => row.Classification == ImportPreviewClassification.TeamUnchanged),
        rows.Count(row => row.Classification == ImportPreviewClassification.TeamUpdatable),
        rows.Count(row => row.Classification is ImportPreviewClassification.TeamSportConflict or ImportPreviewClassification.TeamAmbiguousConflict),
        rows.Count(row => row.Classification == ImportPreviewClassification.StructuralError));

    private static RosterImportPreviewSummary SummarizeRosters(IReadOnlyList<RosterImportPreviewRow> rows) => new(
        rows.Count,
        rows.Count(row => row.Classification == ImportPreviewClassification.PlayerNew),
        rows.Count(row => row.Classification == ImportPreviewClassification.PlayerUpdatable),
        rows.Count(row => row.Classification == ImportPreviewClassification.PlayerUnchanged),
        rows.Count(row => row.Classification == ImportPreviewClassification.PlayerAmbiguousConflict),
        rows.Count(row => row.Classification is ImportPreviewClassification.UnresolvedTeamError or ImportPreviewClassification.StructuralError));

    private sealed record TeamImportResolution(int? TeamId, bool IsNew);
}

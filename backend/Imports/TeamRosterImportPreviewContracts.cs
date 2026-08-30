using System.Text.Json.Serialization;

namespace PlayPredict.Api.Imports;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImportPreviewClassification
{
    TeamNew,
    TeamUnchanged,
    TeamUpdatable,
    TeamSportConflict,
    TeamAmbiguousConflict,
    PlayerNew,
    PlayerUnchanged,
    PlayerUpdatable,
    PlayerAmbiguousConflict,
    UnresolvedTeamError,
    StructuralError
}

public sealed record ImportProposedChange(string Field, string? CurrentValue, string? ProposedValue);

public sealed record TeamImportPreviewRow(
    string Sheet,
    int RowNumber,
    string Entity,
    ImportPreviewClassification Classification,
    string Message,
    string Name,
    string ShortName,
    string Sport,
    int? TeamId,
    IReadOnlyList<ImportProposedChange> ProposedChanges);

public sealed record RosterImportPreviewRow(
    string Sheet,
    int RowNumber,
    string Entity,
    ImportPreviewClassification Classification,
    string Message,
    string ClubName,
    string FirstName,
    string LastName,
    string DisplayName,
    string? Position,
    int? TeamId,
    int? TeamPlayerId,
    IReadOnlyList<ImportProposedChange> ProposedChanges);

public sealed record TeamImportPreviewSummary(
    int Total,
    int New,
    int Unchanged,
    int Updatable,
    int Conflicts,
    int Errors);

public sealed record RosterImportPreviewSummary(
    int Total,
    int New,
    int Updatable,
    int Unchanged,
    int Conflicts,
    int Errors);

public sealed record TeamRosterImportPreviewResult(
    string OriginalSport,
    string Sport,
    string NormalizedSport,
    TeamImportPreviewSummary TeamsSummary,
    RosterImportPreviewSummary RostersSummary,
    IReadOnlyList<TeamImportPreviewRow> Teams,
    IReadOnlyList<RosterImportPreviewRow> Rosters,
    IReadOnlyList<SpreadsheetValidationIssue> Issues)
{
    public bool CanConfirm => Issues.Count == 0
        && Teams.All(row => row.Classification is ImportPreviewClassification.TeamNew
            or ImportPreviewClassification.TeamUnchanged
            or ImportPreviewClassification.TeamUpdatable)
        && Rosters.All(row => row.Classification is ImportPreviewClassification.PlayerNew
            or ImportPreviewClassification.PlayerUnchanged
            or ImportPreviewClassification.PlayerUpdatable);
}

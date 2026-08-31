using System.Text.Json.Serialization;

namespace PlayPredict.Api.Imports;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MatchImportClassification
{
    MatchCreate,
    MatchUpdate,
    MatchUnchanged,
    MatchFinishedConflict,
    MatchTeamChangeConflict,
    MatchRoundChangeConflict,
    UnresolvedTeamError,
    DuplicateMatchRowError,
    StructuralError
}

public sealed record MatchImportPreviewRow(
    string Sheet,
    int RowNumber,
    string Entity,
    MatchImportClassification Classification,
    string Message,
    int? RoundOrder,
    string? RoundName,
    int? RoundId,
    bool RoundIsNew,
    string HomeTeam,
    string AwayTeam,
    int? HomeTeamId,
    int? AwayTeamId,
    DateTime? StartsAtUtc,
    string? Status,
    int? MatchId,
    IReadOnlyList<ImportProposedChange> ProposedChanges);

public sealed record MatchImportPreviewSummary(
    int Total,
    int Create,
    int Update,
    int Unchanged,
    int Conflicts,
    int Errors);

public sealed record MatchImportPreviewResult(
    int EditionId,
    MatchImportPreviewSummary Summary,
    IReadOnlyList<MatchImportPreviewRow> Matches,
    IReadOnlyList<SpreadsheetValidationIssue> Issues)
{
    public bool CanConfirm => Issues.Count == 0
        && Matches.All(row => row.Classification is MatchImportClassification.MatchCreate
            or MatchImportClassification.MatchUpdate
            or MatchImportClassification.MatchUnchanged);
}

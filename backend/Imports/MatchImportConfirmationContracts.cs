namespace PlayPredict.Api.Imports;

public sealed record MatchImportConfirmationResult(
    ImportConfirmationStatus Status,
    string ProcessedHash,
    string Message,
    ImportConfirmationSummary Matches,
    IReadOnlyList<SpreadsheetValidationIssue> Issues)
{
    public bool IsSuccess => Status == ImportConfirmationStatus.Success;
}

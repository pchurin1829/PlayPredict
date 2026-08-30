using System.Text.Json.Serialization;

namespace PlayPredict.Api.Imports;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImportConfirmationStatus
{
    Success,
    Rejected,
    Failed
}

public sealed record ImportConfirmationSummary(int Created, int Updated, int Unchanged);

public sealed record TeamRosterImportConfirmationResult(
    ImportConfirmationStatus Status,
    string ProcessedHash,
    string Message,
    ImportConfirmationSummary Teams,
    ImportConfirmationSummary Rosters,
    IReadOnlyList<SpreadsheetValidationIssue> Issues)
{
    public bool IsSuccess => Status == ImportConfirmationStatus.Success;
}

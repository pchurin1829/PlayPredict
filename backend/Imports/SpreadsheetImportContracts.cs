namespace PlayPredict.Api.Imports;

public enum SpreadsheetImportKind
{
    TeamsAndRosters,
    Matches
}

public enum ImportPlayerPosition
{
    Goalkeeper,
    Defender,
    Midfielder,
    Forward
}

public enum ImportMatchStatus
{
    Scheduled,
    InProgress,
    Suspended,
    Cancelled
}

public sealed record ImportTeamRow(
    int RowNumber,
    string OriginalName,
    string OriginalShortName,
    string Name,
    string ShortName,
    string NormalizedName);

public sealed record ImportRosterRow(
    int RowNumber,
    string OriginalClubName,
    string OriginalFirstName,
    string OriginalLastName,
    string OriginalDisplayName,
    string OriginalPosition,
    string ClubName,
    string FirstName,
    string LastName,
    string DisplayName,
    string NormalizedClubName,
    string NormalizedFirstName,
    string NormalizedLastName,
    ImportPlayerPosition? Position);

public sealed record ImportMatchRow(
    int RowNumber,
    string OriginalRoundNumber,
    string OriginalDate,
    string OriginalTime,
    string OriginalHomeTeam,
    string OriginalAwayTeam,
    string OriginalStatus,
    int? RoundNumber,
    DateOnly? Date,
    TimeOnly? Time,
    string HomeTeam,
    string AwayTeam,
    string NormalizedHomeTeam,
    string NormalizedAwayTeam,
    ImportMatchStatus? Status);

public sealed record SpreadsheetValidationIssue(
    string Code,
    string Message,
    string? SheetName = null,
    int? RowNumber = null,
    string? ColumnName = null);

public sealed record SpreadsheetReadResult(
    IReadOnlyList<ImportTeamRow> Teams,
    IReadOnlyList<ImportRosterRow> Rosters,
    IReadOnlyList<ImportMatchRow> Matches,
    IReadOnlyList<SpreadsheetValidationIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}

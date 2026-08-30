namespace PlayPredict.Api.Imports;

public sealed class TeamRosterImportOptions
{
    public const string SectionName = "TeamRosterImport";
    public const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024;

    public long MaxFileSizeBytes { get; set; } = DefaultMaxFileSizeBytes;
}

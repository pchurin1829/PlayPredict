namespace PlayPredict.Api.Data;

public sealed class LoadTestSeedOptions
{
    public const string SectionName = "LoadTest";
    public const int DefaultUserCount = 100;
    public const int MaximumUserCount = 10_000;

    public bool Enabled { get; init; }
    public int UserCount { get; init; } = DefaultUserCount;
    public string UserPassword { get; init; } = string.Empty;
}

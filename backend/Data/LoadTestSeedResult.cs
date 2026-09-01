namespace PlayPredict.Api.Data;

public sealed record LoadTestSeedResult(
    int UserCount,
    int LeagueId,
    string LeagueName,
    int FinishedMatches,
    int FutureMatches,
    int Predictions,
    int Evaluations);

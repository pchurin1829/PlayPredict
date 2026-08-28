namespace PlayPredict.Api.Dtos;

public record RankingEntryDto(
    int Position,
    int UserId,
    string FirstName,
    string LastName,
    int Points,
    int ExactCount,
    int CorrectCount,
    int IncorrectCount,
    int EvaluatedCount,
    int SharedCount,
    bool IsActiveParticipant);

public record AwardStandingDto(
    int? Position,
    int PositionFrom,
    int PositionTo,
    bool TieBreakPending,
    int UserId,
    string FirstName,
    string LastName,
    int Points,
    int ExactCount,
    int CorrectCount,
    int IncorrectCount,
    int EvaluatedCount,
    int AccumulatedScoreError,
    int PreferredPlayerPoints,
    bool IsActiveParticipant);

public record UserLeaguePositionDto(
    int LeagueId,
    string LeagueName,
    int DensePosition,
    int SharedCount,
    int Points);

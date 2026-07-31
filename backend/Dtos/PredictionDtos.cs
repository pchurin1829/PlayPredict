namespace PlayPredict.Api.Dtos;

public record PredictionDto(
    int Id,
    int MatchId,
    int UserId,
    int PredictedHomeScore,
    int PredictedAwayScore,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    int? Points,
    string? EvaluationType,
    string? EvaluationLabel,
    int? OfficialHomeScore,
    int? OfficialAwayScore);

public record CreatePredictionDto(
    int MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore);

public record UpdatePredictionDto(
    int PredictedHomeScore,
    int PredictedAwayScore);

public record MatchWithPredictionDto(
    int Id,
    int RoundId,
    string ParticipantHome,
    string ParticipantAway,
    DateTime StartsAtUtc,
    string Status,
    int? HomeGoals,
    int? AwayGoals,
    PredictionDto? MyPrediction,
    bool CanPredict);

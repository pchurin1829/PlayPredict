namespace PlayPredict.Api.Dtos;

public record PredictionDto(
    int Id,
    int LeagueId,
    int MatchId,
    int UserId,
    int PredictedHomeScore,
    int PredictedAwayScore,
    int? PreferredPlayerId,
    string? PreferredPlayerName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    int? Points,
    int? ResultPoints,
    int? PreferredPlayerPoints,
    string? EvaluationType,
    string? EvaluationLabel,
    int? OfficialHomeScore,
    int? OfficialAwayScore);

public record CreatePredictionDto(
    int LeagueId,
    int MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore,
    int? PreferredPlayerId);

public record UpdatePredictionDto(
    int PredictedHomeScore,
    int PredictedAwayScore,
    int? PreferredPlayerId);

public record AvailablePlayerDto(int Id, int TeamId, string FirstName, string LastName, string? Nickname, int? ShirtNumber, string Position);

public record MatchWithPredictionDto(
    int Id,
    int RoundId,
    int HomeTeamId,
    int AwayTeamId,
    string ParticipantHome,
    string ParticipantAway,
    DateTime StartsAtUtc,
    string Status,
    int? HomeGoals,
    int? AwayGoals,
    IReadOnlyList<AvailablePlayerDto> HomePlayers,
    IReadOnlyList<AvailablePlayerDto> AwayPlayers,
    bool PreferredPlayerEnabled,
    PredictionDto? MyPrediction,
    bool CanPredict);

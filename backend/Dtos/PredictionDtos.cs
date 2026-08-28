namespace PlayPredict.Api.Dtos;

public record PredictionDto(
    int Id,
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
    int? PreferredPlayerId,
    bool UpdatePreferredPlayer = false);

public record UpdatePredictionDto(
    int LeagueId,
    int PredictedHomeScore,
    int PredictedAwayScore,
    int? PreferredPlayerId,
    bool UpdatePreferredPlayer = false);

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
    IReadOnlyList<AvailablePlayerDto> QuickPreferredPlayers,
    bool PreferredPlayerEnabled,
    bool PredictionEligible,
    PredictionDto? MyPrediction,
    bool CanPredict);

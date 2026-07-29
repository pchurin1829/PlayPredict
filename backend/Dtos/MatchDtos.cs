namespace PlayPredict.Api.Dtos;

public record MatchDto(
    int Id,
    int RoundId,
    string ParticipantHome,
    string ParticipantAway,
    DateTime StartsAtUtc,
    string Status,
    int? HomeGoals,
    int? AwayGoals,
    DateTime CreatedAtUtc);

public record CreateMatchDto(
    string ParticipantHome,
    string ParticipantAway,
    DateTime StartsAtUtc,
    string? Status);

public record UpdateMatchDto(
    string ParticipantHome,
    string ParticipantAway,
    DateTime StartsAtUtc,
    string Status);

public record MatchResultDto(
    int HomeGoals,
    int AwayGoals);

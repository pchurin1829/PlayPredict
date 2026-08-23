namespace PlayPredict.Api.Dtos;

public record MatchDto(
    int Id,
    int RoundId,
    int HomeTeamId,
    int AwayTeamId,
    string ParticipantHome,
    string ParticipantAway,
    string? HomeTeamLogoUrl,
    string? AwayTeamLogoUrl,
    DateTime StartsAtUtc,
    string Status,
    int? HomeGoals,
    int? AwayGoals,
    DateTime CreatedAtUtc);

public record CreateMatchDto(
    int HomeTeamId,
    int AwayTeamId,
    DateTime StartsAtUtc,
    string? Status);

public record UpdateMatchDto(
    int HomeTeamId,
    int AwayTeamId,
    DateTime StartsAtUtc,
    string Status);

public record MatchResultDto(
    int HomeGoals,
    int AwayGoals);

public record TeamDto(int Id, string Name, string ShortName, string? LogoUrl, string Sport, bool Active);

public record SaveTeamDto(string Name, string ShortName, string? LogoUrl, string? Sport, bool Active);

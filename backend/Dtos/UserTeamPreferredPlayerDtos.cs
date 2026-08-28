namespace PlayPredict.Api.Dtos;

public record SaveUserTeamPreferredPlayerDto(int TeamPlayerId);

public record UserTeamPreferredPlayerDto(
    int Id,
    int TeamId,
    string TeamName,
    int TeamPlayerId,
    string TeamPlayerName,
    bool IsValid,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record PreferredPlayerProfileTeamDto(
    int TeamId,
    string TeamName,
    string TeamShortName,
    IReadOnlyList<PreferredPlayerProfilePlayerDto> Players,
    UserTeamPreferredPlayerDto? Preference);

public record PreferredPlayerProfilePlayerDto(int Id, string Name);

namespace PlayPredict.Api.Dtos;

public record CreateLeagueDto(
    string Name,
    string? Description,
    int CompetitionId,
    string ScopeType,
    int? RoundFromId,
    int? RoundToId);

public record UpdateLeagueDto(
    string Name,
    string? Description,
    bool IsActive);

public record JoinLeagueDto(string InviteCode);

public record RoundSummaryDto(int Id, string Name, int Order);

public record LeagueSummaryDto(
    int Id,
    string Name,
    string? Description,
    int CompetitionId,
    string CompetitionName,
    string ScopeType,
    int? RoundFromId,
    int? RoundToId,
    string? RoundFromName,
    string? RoundToName,
    int CreatedByUserId,
    bool IsCreator,
    int ParticipantsCount,
    bool IsActive,
    string? InviteCode);

public record LeagueDetailDto(
    int Id,
    string Name,
    string? Description,
    int CompetitionId,
    string CompetitionName,
    string ScopeType,
    int? RoundFromId,
    int? RoundToId,
    string? RoundFromName,
    string? RoundToName,
    int CreatedByUserId,
    string CreatedByName,
    bool IsCreator,
    int ParticipantsCount,
    bool IsActive,
    string? InviteCode,
    List<RoundSummaryDto> Rounds);

public record LeagueParticipantDto(
    int UserId,
    string FirstName,
    string LastName,
    DateTime JoinedAtUtc,
    bool IsCreator);

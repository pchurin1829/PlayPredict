namespace PlayPredict.Api.Dtos;

public record RoundDto(
    int Id,
    int EditionId,
    string Name,
    int Order,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc);

public record CreateRoundDto(
    string Name,
    int Order,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc);

public record UpdateRoundDto(
    string Name,
    int Order,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc);

public record GenerateRoundsDto(int Count);

public record GenerateRoundsResultDto(int ExistingCount, int CreatedCount, int TotalCount, string Message, List<RoundDto> Rounds);

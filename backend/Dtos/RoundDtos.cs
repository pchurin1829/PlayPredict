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

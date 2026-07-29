namespace PlayPredict.Api.Dtos;

public record EditionDto(
    int Id,
    int CompetitionId,
    string Name,
    DateTime StartDateUtc,
    DateTime? EndDateUtc,
    string Status,
    DateTime CreatedAtUtc);

public record CreateEditionDto(
    string Name,
    DateTime StartDateUtc,
    DateTime? EndDateUtc,
    string? Status);

public record UpdateEditionDto(
    string Name,
    DateTime StartDateUtc,
    DateTime? EndDateUtc,
    string Status);

namespace PlayPredict.Api.Dtos;

public record CompetitionDto(
    int Id,
    string Name,
    string? Description,
    string Sport,
    bool IsActive,
    DateTime CreatedAtUtc);

public record CreateCompetitionDto(
    string Name,
    string? Description,
    string Sport,
    bool IsActive = true);

public record UpdateCompetitionDto(
    string Name,
    string? Description,
    string Sport,
    bool IsActive);

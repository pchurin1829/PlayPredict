namespace PlayPredict.Api.Dtos;

public record CompetitionDto(
    int Id,
    int ExperienceId,
    string Name,
    string? Description,
    string Sport,
    bool IsActive,
    DateTime CreatedAtUtc);

public record CreateCompetitionDto(
    string Name,
    string? Description,
    string Sport,
    bool IsActive = true,
    // Obligatorio al crear; nullable para devolver un error de validación por campo si falta.
    int? ExperienceId = null);

public record UpdateCompetitionDto(
    string Name,
    string? Description,
    string Sport,
    bool IsActive,
    int? ExperienceId = null);

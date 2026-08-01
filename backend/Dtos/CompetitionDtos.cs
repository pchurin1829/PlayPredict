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
    // Opcional: si no se indica, se asocia a la Experience "PlayPredict Demo" para no
    // romper el flujo existente de alta de Competencias (Sprints 1 a 7 sin cambios).
    int? ExperienceId = null);

public record UpdateCompetitionDto(
    string Name,
    string? Description,
    string Sport,
    bool IsActive,
    int? ExperienceId = null);

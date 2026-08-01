namespace PlayPredict.Api.Dtos;

public record ExperienceDto(
    int Id,
    string Name,
    string? Description,
    string Status,
    string StatusLabel,
    string? PrimaryColor,
    string? SecondaryColor,
    string? LogoUrl,
    bool IsPublic,
    int DefaultExactScorePoints,
    int DefaultCorrectOutcomePoints,
    int DefaultIncorrectPoints,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CreateExperienceDto(
    string Name,
    string? Description,
    string? PrimaryColor,
    string? SecondaryColor,
    string? LogoUrl,
    bool IsPublic,
    int DefaultExactScorePoints,
    int DefaultCorrectOutcomePoints,
    int DefaultIncorrectPoints);

public record UpdateExperienceDto(
    string Name,
    string? Description,
    string? PrimaryColor,
    string? SecondaryColor,
    string? LogoUrl,
    bool IsPublic,
    int DefaultExactScorePoints,
    int DefaultCorrectOutcomePoints,
    int DefaultIncorrectPoints);

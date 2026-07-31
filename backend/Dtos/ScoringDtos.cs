namespace PlayPredict.Api.Dtos;

public record EditionScoringConfigurationDto(
    int Id,
    int EditionId,
    int ExactScorePoints,
    int CorrectOutcomePoints,
    int IncorrectPoints,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record UpdateEditionScoringConfigurationDto(
    int ExactScorePoints,
    int CorrectOutcomePoints,
    int IncorrectPoints);

namespace PlayPredict.Api.Dtos;

public record EditionScoringConfigurationDto(
    int Id,
    int EditionId,
    int ExactScorePoints,
    int CorrectOutcomePoints,
    int IncorrectPoints,
    bool UseExperienceDefaults,
    // Valores efectivamente aplicados por el Motor de Puntuación: son los propios cuando
    // UseExperienceDefaults es false, o los de la Experience (heredados por completo)
    // cuando es true. El frontend nunca decide esto, solo muestra lo que llega.
    int EffectiveExactScorePoints,
    int EffectiveCorrectOutcomePoints,
    int EffectiveIncorrectPoints,
    bool PreferredPlayerEnabled,
    int PreferredPlayerPointsPerGoal,
    IReadOnlyList<string> PreferredPlayerPositions,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record UpdateEditionScoringConfigurationDto(
    int ExactScorePoints,
    int CorrectOutcomePoints,
    int IncorrectPoints,
    bool UseExperienceDefaults,
    bool PreferredPlayerEnabled,
    int PreferredPlayerPointsPerGoal,
    IReadOnlyList<string> PreferredPlayerPositions);

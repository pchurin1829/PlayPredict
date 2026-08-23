namespace PlayPredict.Api.Domain.Entities;

public class EditionScoringConfiguration
{
    public int Id { get; set; }
    public int EditionId { get; set; }
    public int ExactScorePoints { get; set; }
    public int CorrectOutcomePoints { get; set; }
    public int IncorrectPoints { get; set; }

    // Sprint 8: si es true, la Edición hereda por completo los valores por defecto de la
    // Experience de su Competencia (vía Competition.ExperienceId), ignorando los propios.
    // Sin mezcla parcial: la herencia es total. Por defecto false, para no alterar el
    // comportamiento de ninguna Edición existente de los Sprints 1 a 7.
    public bool UseExperienceDefaults { get; set; }
    public bool PreferredPlayerEnabled { get; set; } = true;
    public int PreferredPlayerPointsPerGoal { get; set; } = 2;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Edition Edition { get; set; } = null!;
}

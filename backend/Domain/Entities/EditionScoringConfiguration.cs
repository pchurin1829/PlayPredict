namespace PlayPredict.Api.Domain.Entities;

public class EditionScoringConfiguration
{
    public int Id { get; set; }
    public int EditionId { get; set; }
    public int ExactScorePoints { get; set; }
    public int CorrectOutcomePoints { get; set; }
    public int IncorrectPoints { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Edition Edition { get; set; } = null!;
}

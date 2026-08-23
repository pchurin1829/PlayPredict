using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Domain.Entities;

public class PredictionEvaluation
{
    public int Id { get; set; }
    public int PredictionId { get; set; }
    public int Points { get; set; }
    public int ResultPoints { get; set; }
    public int PreferredPlayerPoints { get; set; }
    public EvaluationType EvaluationType { get; set; }
    public int OfficialHomeScore { get; set; }
    public int OfficialAwayScore { get; set; }
    public int AppliedRuleValue { get; set; }
    public DateTime EvaluatedAtUtc { get; set; }

    public Prediction Prediction { get; set; } = null!;
}

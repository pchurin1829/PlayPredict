namespace PlayPredict.Api.Domain.Entities;

public class Prediction
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int UserId { get; set; }
    public int PredictedHomeScore { get; set; }
    public int PredictedAwayScore { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Match Match { get; set; } = null!;
    public User User { get; set; } = null!;
}

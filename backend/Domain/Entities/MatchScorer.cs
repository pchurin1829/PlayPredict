namespace PlayPredict.Api.Domain.Entities;

public class MatchScorer
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int TeamPlayerId { get; set; }
    public int Goals { get; set; }
    public Match Match { get; set; } = null!;
    public TeamPlayer TeamPlayer { get; set; } = null!;
}

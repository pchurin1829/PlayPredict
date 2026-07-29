namespace PlayPredict.Api.Domain.Entities;

public class Round
{
    public int Id { get; set; }
    public int EditionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }

    public Edition Edition { get; set; } = null!;
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}

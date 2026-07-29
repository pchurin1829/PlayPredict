using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Domain.Entities;

public class Edition
{
    public int Id { get; set; }
    public int CompetitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public EditionStatus Status { get; set; } = EditionStatus.Draft;
    public DateTime CreatedAtUtc { get; set; }

    public Competition Competition { get; set; } = null!;
    public ICollection<Round> Rounds { get; set; } = new List<Round>();
}

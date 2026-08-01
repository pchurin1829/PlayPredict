namespace PlayPredict.Api.Domain.Entities;

public class Competition
{
    public int Id { get; set; }
    public int ExperienceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Sport { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }

    public Experience Experience { get; set; } = null!;
    public ICollection<Edition> Editions { get; set; } = new List<Edition>();
}

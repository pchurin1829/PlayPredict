namespace PlayPredict.Api.Domain.Entities;

public class TeamPlayer
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int? ShirtNumber { get; set; }
    public string? Position { get; set; }
    public bool Active { get; set; } = true;
    public string? PhotoUrl { get; set; }
    public Team Team { get; set; } = null!;
}

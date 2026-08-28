namespace PlayPredict.Api.Domain.Entities;

public class UserTeamPreferredPlayer
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TeamId { get; set; }
    public int TeamPlayerId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public User User { get; set; } = null!;
    public Team Team { get; set; } = null!;
    public TeamPlayer TeamPlayer { get; set; } = null!;
}

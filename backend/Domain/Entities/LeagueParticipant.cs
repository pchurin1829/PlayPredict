namespace PlayPredict.Api.Domain.Entities;

public class LeagueParticipant
{
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAtUtc { get; set; }
    public DateTime? LeftAtUtc { get; set; }

    public League League { get; set; } = null!;
    public User User { get; set; } = null!;
}

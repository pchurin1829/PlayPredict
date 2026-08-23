using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Domain.Entities;

public class Match
{
    public int Id { get; set; }
    public int RoundId { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public string ParticipantHome { get; set; } = string.Empty;
    public string ParticipantAway { get; set; } = string.Empty;
    public DateTime StartsAtUtc { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
    public int? HomeGoals { get; set; }
    public int? AwayGoals { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Round Round { get; set; } = null!;
    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;
}

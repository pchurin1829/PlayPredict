using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Domain.Entities;

public class League
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CompetitionId { get; set; }
    public int EditionId { get; set; }
    public LeagueScopeType ScopeType { get; set; }
    public int? RoundFromId { get; set; }
    public int? RoundToId { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public LeagueType LeagueType { get; set; } = LeagueType.Private;
    public int? SourceLeagueId { get; set; }
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool UseGeneralScoring { get; set; } = true;
    public int ExactScorePoints { get; set; } = 6;
    public int CorrectOutcomePoints { get; set; } = 3;
    public int IncorrectPoints { get; set; }
    public bool PreferredPlayerEnabled { get; set; } = true;
    public int PreferredPlayerPointsPerGoal { get; set; } = 2;
    public PlayerPosition PreferredPlayerPositions { get; set; } = PlayerPosition.Midfielder | PlayerPosition.Forward;

    public Competition Competition { get; set; } = null!;
    public Edition Edition { get; set; } = null!;
    public Round? RoundFrom { get; set; }
    public Round? RoundTo { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public League? SourceLeague { get; set; }
    public ICollection<League> DerivedLeagues { get; set; } = new List<League>();
    public ICollection<LeagueParticipant> Participants { get; set; } = new List<LeagueParticipant>();
}

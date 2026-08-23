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
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Competition Competition { get; set; } = null!;
    public Edition Edition { get; set; } = null!;
    public Round? RoundFrom { get; set; }
    public Round? RoundTo { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<LeagueParticipant> Participants { get; set; } = new List<LeagueParticipant>();
}

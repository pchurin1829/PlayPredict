using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Domain.Entities;

// El Premio no calcula puntos ni posiciones: solo describe qué se entrega y a quién
// corresponde según el Ranking (RankingService / PrizeWinnerService).
public class Prize
{
    public int Id { get; set; }
    public int EditionId { get; set; }
    public int? RoundId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PrizeType PrizeType { get; set; }
    public string? ReferenceValue { get; set; }
    public string? SponsorName { get; set; }
    public string? ImageUrl { get; set; }
    public PrizeScopeType ScopeType { get; set; }
    public PrizeAwardCriteria AwardCriteria { get; set; }
    public int? PositionFrom { get; set; }
    public int? PositionTo { get; set; }
    public PrizeStatus Status { get; set; } = PrizeStatus.Draft;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Edition Edition { get; set; } = null!;
    public Round? Round { get; set; }
}

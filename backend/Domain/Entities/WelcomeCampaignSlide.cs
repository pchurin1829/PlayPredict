using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Domain.Entities;

public class WelcomeCampaignSlide
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public string ImageKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal DurationSeconds { get; set; }
    public WelcomeCampaignFitMode FitMode { get; set; }
    public int OriginalWidth { get; set; }
    public int OriginalHeight { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public WelcomeCampaign Campaign { get; set; } = null!;
}

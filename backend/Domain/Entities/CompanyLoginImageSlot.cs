using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Domain.Entities;

public class CompanyLoginImageSlot
{
    public int CompanyId { get; set; }
    public LoginImageSlot Slot { get; set; }
    public string? ImageKey { get; set; }
    public LoginImageFitMode FitMode { get; set; }
    public int? OriginalWidth { get; set; }
    public int? OriginalHeight { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int? UpdatedByUserId { get; set; }

    public Company Company { get; set; } = null!;
    public User? UpdatedByUser { get; set; }
}

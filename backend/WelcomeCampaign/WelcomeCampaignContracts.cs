namespace PlayPredict.Api.WelcomeCampaigns;

public sealed record WelcomeCampaignWarningDto(string Code, string Message);

public sealed record WelcomeCampaignSlideDto(
    int Id, string ImageUrl, int SortOrder, decimal DurationSeconds, string FitMode,
    int OriginalWidth, int OriginalHeight, DateTime UpdatedAtUtc, IReadOnlyList<WelcomeCampaignWarningDto> Warnings);

public sealed record WelcomeCampaignDto(
    int Id, string Name, bool IsActive, DateTime? ValidFromUtc, DateTime? ValidToUtc,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, IReadOnlyList<WelcomeCampaignSlideDto> Slides);

public sealed record CreateWelcomeCampaignRequest(string Name, DateTime? ValidFromUtc, DateTime? ValidToUtc);
public sealed record UpdateWelcomeCampaignRequest(string Name, DateTime? ValidFromUtc, DateTime? ValidToUtc);
public sealed record UpdateWelcomeCampaignSlideRequest(decimal DurationSeconds, string FitMode);
public sealed record ReorderWelcomeCampaignSlideRequest(int SortOrder);

public sealed record ActiveWelcomeCampaignSlideDto(int Id, string ImageUrl, int SortOrder, decimal DurationSeconds, string FitMode);
public sealed record ActiveWelcomeCampaignDto(int CampaignId, string Name, IReadOnlyList<ActiveWelcomeCampaignSlideDto> Slides);

public sealed record WelcomeCampaignImageValidationResult(
    bool IsValid, string? ErrorCode, string? ErrorMessage, byte[]? Content, string? Extension,
    int Width, int Height, IReadOnlyList<WelcomeCampaignWarningDto> Warnings);

public sealed class WelcomeCampaignValidationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>
/// Dos activaciones concurrentes chocaron contra el índice único parcial
/// IX_WelcomeCampaigns_CompanyId_ActiveOnly (a lo sumo una campaña activa por Company).
/// Se traduce a 409 Conflict, no a un 400 de validación.
/// </summary>
public sealed class WelcomeCampaignConcurrencyException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

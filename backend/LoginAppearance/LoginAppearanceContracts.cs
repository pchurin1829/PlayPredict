using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.LoginAppearance;

public sealed record LoginAppearanceImageDto(string ImageUrl, string FitMode);
public sealed record PublicLoginAppearanceDto(string Version, LoginAppearanceImageDto Main,
    LoginAppearanceImageDto AdTop, LoginAppearanceImageDto AdMiddle, LoginAppearanceImageDto AdBottom);
public sealed record LoginAppearanceWarningDto(string Code, string Message);
public sealed record AdminLoginAppearanceSlotDto(string Slot, string EffectiveImageUrl, bool IsDefault,
    string FitMode, DateTime? UpdatedAtUtc, int OriginalWidth, int OriginalHeight, double AspectRatio,
    double RecommendedAspectRatio, IReadOnlyList<LoginAppearanceWarningDto> Warnings);
public sealed record UpdateLoginImageFitModeRequest(string FitMode);
public sealed record LoginImageValidationResult(bool IsValid, string? ErrorCode, string? ErrorMessage,
    byte[]? Content, string? Extension, int Width, int Height, IReadOnlyList<LoginAppearanceWarningDto> Warnings);

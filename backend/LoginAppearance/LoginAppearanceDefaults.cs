using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.LoginAppearance;

public sealed record LoginAppearanceSlotDefault(
    LoginImageSlot Slot, string ImageUrl, LoginImageFitMode FitMode,
    int Width, int Height, int MinimumWidth, int MinimumHeight,
    int RecommendedWidth, int RecommendedHeight);

public static class LoginAppearanceDefaults
{
    public const double RecommendedAspectRatio = 4d / 3d;
    public const double AspectRatioWarningThreshold = .05d;

    public static readonly IReadOnlyDictionary<LoginImageSlot, LoginAppearanceSlotDefault> Slots =
        new Dictionary<LoginImageSlot, LoginAppearanceSlotDefault>
        {
            [LoginImageSlot.Main] = new(LoginImageSlot.Main, "/assets/el-nene-login/copa-el-nene-panel-principal.png", LoginImageFitMode.Contain, 1122, 1402, 1024, 768, 1440, 1080),
            [LoginImageSlot.AdTop] = new(LoginImageSlot.AdTop, "/assets/el-nene-login/producto-1.png", LoginImageFitMode.Cover, 1536, 1024, 480, 360, 960, 720),
            [LoginImageSlot.AdMiddle] = new(LoginImageSlot.AdMiddle, "/assets/el-nene-login/producto-2.png", LoginImageFitMode.Cover, 1536, 1024, 480, 360, 960, 720),
            [LoginImageSlot.AdBottom] = new(LoginImageSlot.AdBottom, "/assets/el-nene-login/producto-3.png", LoginImageFitMode.Cover, 1536, 1024, 480, 360, 960, 720)
        };
}

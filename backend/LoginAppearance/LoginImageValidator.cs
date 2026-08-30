using PlayPredict.Api.Domain.Enums;
using SkiaSharp;

namespace PlayPredict.Api.LoginAppearance;

public sealed class LoginImageValidator
{
    public const long MaximumBytes = 8L * 1024 * 1024;
    public const int MaximumSide = 8192;
    public const long MaximumPixels = 40_000_000;

    public async Task<LoginImageValidationResult> ValidateAsync(Stream input, long declaredLength, LoginImageSlot slot, CancellationToken cancellationToken = default)
    {
        if (declaredLength <= 0) return Error("FILE_EMPTY", "El archivo está vacío.");
        if (declaredLength > MaximumBytes) return Error("FILE_TOO_LARGE", "La imagen no puede superar 8 MB.");

        await using var buffer = new MemoryStream((int)declaredLength);
        await input.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > MaximumBytes) return Error("FILE_TOO_LARGE", "La imagen no puede superar 8 MB.");
        var bytes = buffer.ToArray();

        try
        {
            using var data = SKData.CreateCopy(bytes);
            using var codec = SKCodec.Create(data);
            if (codec is null) return Error("INVALID_IMAGE", "El archivo no contiene una imagen decodificable.");
            var extension = codec.EncodedFormat switch
            {
                SKEncodedImageFormat.Png => ".png",
                SKEncodedImageFormat.Jpeg => ".jpg",
                SKEncodedImageFormat.Webp => ".webp",
                _ => null
            };
            if (extension is null) return Error("UNSUPPORTED_IMAGE_FORMAT", "Usá una imagen PNG, JPEG o WebP.");
            var width = codec.Info.Width;
            var height = codec.Info.Height;
            if (width <= 0 || height <= 0) return Error("INVALID_IMAGE", "La imagen no tiene dimensiones válidas.");
            if (width > MaximumSide || height > MaximumSide || (long)width * height > MaximumPixels)
                return Error("IMAGE_DIMENSIONS_TOO_LARGE", "La imagen supera 8192 px por lado o 40 megapíxeles.");
            using var bitmap = SKBitmap.Decode(bytes);
            if (bitmap is null) return Error("INVALID_IMAGE", "La imagen no pudo decodificarse completamente.");
            return new(true, null, null, bytes, extension, width, height, BuildWarnings(slot, width, height));
        }
        catch
        {
            return Error("INVALID_IMAGE", "El archivo no contiene una imagen válida.");
        }
    }

    public static IReadOnlyList<LoginAppearanceWarningDto> BuildWarnings(LoginImageSlot slot, int width, int height)
    {
        var defaults = LoginAppearanceDefaults.Slots[slot];
        var warnings = new List<LoginAppearanceWarningDto>();
        if (width < defaults.MinimumWidth || height < defaults.MinimumHeight)
            warnings.Add(new("LOW_RESOLUTION", $"Resolución inferior a la mínima recomendada de {defaults.MinimumWidth}×{defaults.MinimumHeight}."));
        var ratio = (double)width / height;
        if (Math.Abs(ratio / LoginAppearanceDefaults.RecommendedAspectRatio - 1) > LoginAppearanceDefaults.AspectRatioWarningThreshold)
            warnings.Add(new("ASPECT_RATIO_MISMATCH", "La proporción difiere más de 5% de la recomendada 4:3 y puede dejar márgenes o requerir recorte."));
        return warnings;
    }

    private static LoginImageValidationResult Error(string code, string message) => new(false, code, message, null, null, 0, 0, []);
}

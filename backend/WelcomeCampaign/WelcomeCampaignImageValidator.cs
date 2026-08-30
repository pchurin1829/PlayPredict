using SkiaSharp;

namespace PlayPredict.Api.WelcomeCampaigns;

public sealed class WelcomeCampaignImageValidator
{
    public const long MaximumBytes = 5L * 1024 * 1024;
    public const int MaximumSide = 8192;
    public const long MaximumPixels = 40_000_000;
    private const double RecommendedAspectRatio = 4d / 3d;
    private const double AspectRatioWarningThreshold = .25d;

    public async Task<WelcomeCampaignImageValidationResult> ValidateAsync(Stream input, long declaredLength, CancellationToken cancellationToken = default)
    {
        if (declaredLength <= 0) return Error("FILE_EMPTY", "El archivo está vacío.");
        if (declaredLength > MaximumBytes) return Error("FILE_TOO_LARGE", "La imagen no puede superar 5 MB.");

        await using var buffer = new MemoryStream((int)declaredLength);
        await input.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > MaximumBytes) return Error("FILE_TOO_LARGE", "La imagen no puede superar 5 MB.");
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
            return new(true, null, null, bytes, extension, width, height, BuildWarnings(width, height));
        }
        catch
        {
            return Error("INVALID_IMAGE", "El archivo no contiene una imagen válida.");
        }
    }

    public static IReadOnlyList<WelcomeCampaignWarningDto> BuildWarnings(int width, int height)
    {
        var ratio = (double)width / height;
        if (Math.Abs(ratio / RecommendedAspectRatio - 1) > AspectRatioWarningThreshold)
            return [new("ASPECT_RATIO_MISMATCH", "La proporción difiere bastante de 4:3. No es obligatorio, pero puede dejar márgenes (Mostrar completa) o recortar más de lo esperado (Cubrir panel).")];
        return [];
    }

    private static WelcomeCampaignImageValidationResult Error(string code, string message) => new(false, code, message, null, null, 0, 0, []);
}

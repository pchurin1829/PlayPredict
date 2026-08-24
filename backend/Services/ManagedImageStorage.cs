namespace PlayPredict.Api.Services;

public static class ManagedImageStorage
{
    public const long MaxOriginalBytes = 8 * 1024 * 1024;
    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public static string GetRoot(IConfiguration configuration, IWebHostEnvironment environment) =>
        configuration["UploadStorage:RootPath"] ?? Path.Combine(environment.ContentRootPath, "storage", "uploads");

    public static async Task<(string? Url, string? Error)> SaveAsync(
        IFormFile file, string category, string prefix, IConfiguration configuration, IWebHostEnvironment environment)
    {
        if (file.Length == 0 || file.Length > MaxOriginalBytes)
            return (null, "La imagen debe pesar menos de 8 MB.");
        if (!Extensions.TryGetValue(file.ContentType, out var extension))
            return (null, "Usá una imagen JPG, PNG o WEBP.");

        await using var input = new MemoryStream();
        await file.CopyToAsync(input);
        input.Position = 0;
        var signature = new byte[12];
        var bytesRead = await input.ReadAsync(signature);
        if (!HasValidSignature(signature.AsSpan(0, bytesRead), file.ContentType))
            return (null, "El archivo no contiene una imagen válida.");
        input.Position = 0;

        var root = GetRoot(configuration, environment);
        var directory = Path.Combine(root, category);
        Directory.CreateDirectory(directory);
        var fileName = $"{prefix}-{Guid.NewGuid():N}{extension}";
        await using (var output = File.Create(Path.Combine(directory, fileName))) await input.CopyToAsync(output);
        return ($"/api/uploads/{category}/{fileName}", null);
    }

    public static void Delete(string? url, string category, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var prefix = $"/api/uploads/{category}/";
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(prefix, StringComparison.Ordinal)) return;
        var path = Path.Combine(GetRoot(configuration, environment), category, Path.GetFileName(url));
        if (File.Exists(path)) File.Delete(path);
    }

    public static void CopyLegacyFiles(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var legacyRoot = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");
        if (!Directory.Exists(legacyRoot)) return;
        var targetRoot = GetRoot(configuration, environment);
        foreach (var source in Directory.EnumerateFiles(legacyRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(legacyRoot, source);
            var target = Path.Combine(targetRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (!File.Exists(target)) File.Copy(source, target);
        }
    }

    private static bool HasValidSignature(ReadOnlySpan<byte> bytes, string contentType) => contentType switch
    {
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        "image/png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        "image/webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8),
        _ => false
    };
}

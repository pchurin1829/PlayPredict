using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.LoginAppearance;

public sealed class LocalLoginImageStorage(IConfiguration configuration, IWebHostEnvironment environment) : ILoginImageStorage
{
    private readonly string root = Path.GetFullPath(ManagedImageStorage.GetRoot(configuration, environment));

    public async Task<string> SaveAsync(int companyId, LoginImageSlot slot, byte[] content, string extension, CancellationToken cancellationToken = default)
    {
        var key = $"login-appearance/{companyId}/{slot.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}{extension}";
        var path = ResolveSafePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await output.WriteAsync(content, cancellationToken);
        return key;
    }

    public Task DeleteAsync(string? imageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageKey) || !imageKey.StartsWith("login-appearance/", StringComparison.Ordinal)) return Task.CompletedTask;
        var path = ResolveSafePath(imageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string imageKey) => $"/api/uploads/{imageKey.Replace('\\', '/')}";

    private string ResolveSafePath(string key)
    {
        var path = Path.GetFullPath(Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid image storage key.");
        return path;
    }
}

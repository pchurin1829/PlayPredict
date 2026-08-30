using System.Security.Cryptography;

namespace PlayPredict.Api.Imports;

public static class SpreadsheetFileHash
{
    public static string ComputeSha256(ReadOnlySpan<byte> content) =>
        Convert.ToHexString(SHA256.HashData(content));

    public static async Task<(byte[] Content, string Sha256)> ReadAndComputeSha256Async(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (stream.CanSeek) stream.Position = 0;
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        var content = buffer.ToArray();
        return (content, ComputeSha256(content));
    }
}

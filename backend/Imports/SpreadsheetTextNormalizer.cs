using System.Text.RegularExpressions;

namespace PlayPredict.Api.Imports;

public static partial class SpreadsheetTextNormalizer
{
    public static string Clean(string? value) => MultipleSpaces().Replace(value?.Trim() ?? string.Empty, " ");

    public static string Normalize(string? value) => Clean(value).ToUpperInvariant();

    public static bool EqualsNormalized(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpaces();
}

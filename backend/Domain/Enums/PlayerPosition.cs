namespace PlayPredict.Api.Domain.Enums;

[Flags]
public enum PlayerPosition
{
    None = 0,
    Goalkeeper = 1,
    Defender = 2,
    Midfielder = 4,
    Forward = 8,
    All = Goalkeeper | Defender | Midfielder | Forward
}

public static class PlayerPositionCatalog
{
    public static readonly IReadOnlyDictionary<PlayerPosition, string> Labels =
        new Dictionary<PlayerPosition, string>
        {
            [PlayerPosition.Goalkeeper] = "Arquero",
            [PlayerPosition.Defender] = "Defensor",
            [PlayerPosition.Midfielder] = "Mediocampista",
            [PlayerPosition.Forward] = "Delantero"
        };

    public static bool TryParse(string? label, out PlayerPosition position)
    {
        var match = Labels.FirstOrDefault(x => string.Equals(x.Value, label?.Trim(), StringComparison.OrdinalIgnoreCase));
        position = match.Key;
        return position != PlayerPosition.None;
    }

    public static IReadOnlyList<string> ToLabels(PlayerPosition positions) =>
        Labels.Where(x => positions.HasFlag(x.Key)).Select(x => x.Value).ToList();
}

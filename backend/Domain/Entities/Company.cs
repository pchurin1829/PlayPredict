namespace PlayPredict.Api.Domain.Entities;

using PlayPredict.Api.Domain.Enums;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public int GeneralExactScorePoints { get; set; } = 6;
    public int GeneralCorrectOutcomePoints { get; set; } = 3;
    public int GeneralIncorrectPoints { get; set; }
    public bool GeneralPreferredPlayerEnabled { get; set; } = true;
    public int GeneralPreferredPlayerPointsPerGoal { get; set; } = 2;
    public PlayerPosition GeneralPreferredPlayerPositions { get; set; } = PlayerPosition.Midfielder | PlayerPosition.Forward;

    public ICollection<User> Users { get; set; } = new List<User>();
}

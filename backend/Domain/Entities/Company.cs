namespace PlayPredict.Api.Domain.Entities;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}

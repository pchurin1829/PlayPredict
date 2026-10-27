namespace PlayPredict.Api.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastAccessUtc { get; set; }
    // P2.1 — seguridad de cuenta. Valores por defecto seguros para filas existentes.
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutUntilUtc { get; set; }
    public int TokenVersion { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? AdultConfirmedAtUtc { get; set; }
    public string? AcceptedTermsVersion { get; set; }
    public DateTime? TermsAcceptedAtUtc { get; set; }
    public string? AcceptedPrivacyVersion { get; set; }
    public DateTime? PrivacyAcceptedAtUtc { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

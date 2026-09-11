namespace PlayPredict.Api.Dtos;

public record UserDto(
    int Id,
    int CompanyId,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastAccessUtc,
    IReadOnlyList<string> Roles,
    bool MustChangePassword = false);

public record UpdateProfileDto(
    string FirstName,
    string LastName);

public record ChangeEmailDto(string NewEmail, string ConfirmEmail, string CurrentPassword);

public record UpdateUserStatusDto(
    bool IsActive);

using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace PlayPredict.Api.Services;

public static class EmailIdentity
{
    public static string Normalize(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    public static bool IsValid(string email) =>
        email.Length <= 200 && !email.Any(char.IsWhiteSpace) &&
        MailAddress.TryCreate(email, out var address) && address.Address == email &&
        address.Host.Contains('.') && !address.Host.StartsWith('.') && !address.Host.EndsWith('.');

    public static Dictionary<string, string[]> Validate(string? email, string? confirmation,
        string emailField = "email", string confirmationField = "confirmEmail")
    {
        var errors = new Dictionary<string, string[]>();
        var normalized = Normalize(email);
        if (!IsValid(normalized)) errors[emailField] = ["Ingresá un email válido (máximo 200 caracteres)."];
        if (!IsValid(Normalize(confirmation))) errors[confirmationField] = ["Confirmá el email con un formato válido."];
        else if (normalized != Normalize(confirmation)) errors[confirmationField] = ["Los emails no coinciden."];
        return errors;
    }

    // The existing unique index also protects concurrent normalized writes.
    public static bool IsDuplicate(DbUpdateException error) =>
        error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Users_Email" };

    public static Dictionary<string, string[]> DuplicateError(string field = "email") =>
        new() { [field] = ["Ya existe un usuario registrado con este email."] };
}

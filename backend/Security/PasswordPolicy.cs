namespace PlayPredict.Api.Security;

/// <summary>
/// Única política de contraseñas de PlayPredict (P2.1).
/// Se aplica al CREAR o CAMBIAR una contraseña, nunca al verificar un hash existente.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 10;
    public const int MaxLength = 200;

    public static Dictionary<string, string[]> Validate(string? password, string field = "password")
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            errors[field] = [$"La contraseña debe tener al menos {MinLength} caracteres."];
        else if (password.Length > MaxLength)
            errors[field] = [$"La contraseña no puede superar los {MaxLength} caracteres."];
        return errors;
    }
}

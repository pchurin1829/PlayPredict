namespace PlayPredict.Api.Dtos;

public record RegisterDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? ConfirmEmail = null);

public record LoginDto(
    string Email,
    string Password);

public record AuthResponseDto(
    string Token,
    UserDto User);

namespace RentAll.Api.Dtos.Auth;

public class ConfirmPasswordDto
{
    public string Password { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(Password))
            return (false, "Password is required");

        return (true, null);
    }
}

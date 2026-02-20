namespace RentAll.Api.Dtos.Auth;

public class UpdatePasswordDto
{
    public string Password { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(Password))
            return (false, "Current password is required");

        if (string.IsNullOrWhiteSpace(NewPassword))
            return (false, "New password is required");

        return (true, null);
    }
}

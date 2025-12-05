using RentAll.Domain.Models.Users;

namespace RentAll.Api.Dtos.Users;

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(Username))
            return (false, "Username is required");

        if (string.IsNullOrWhiteSpace(FirstName))
            return (false, "First Name is required");

        if (string.IsNullOrWhiteSpace(LastName))
            return (false, "Last Name is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        if (string.IsNullOrWhiteSpace(Password))
            return (false, "Password is required");

        if (Password.Length < 6)
            return (false, "Password must be at least 6 characters");

        return (true, null);
    }

    public User ToModel(string passwordHash, Guid currentUser)
    {
        var fullName = $"{FirstName} {LastName}".Trim();
        return new User
        {
            Username = Username,
            FirstName = FirstName,
            LastName = LastName,
            FullName = fullName,
            Email = Email,
            PasswordHash = passwordHash,
            IsActive = 1,
            CreatedBy = currentUser
        };
    }
}
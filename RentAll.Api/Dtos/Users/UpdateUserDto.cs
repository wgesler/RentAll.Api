using RentAll.Domain.Models.Users;

namespace RentAll.Api.Dtos.Users;

public class UpdateUserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
    {
        if (id == Guid.Empty)
            return (false, "User ID is required");

        if (UserId != id)
            return (false, "User ID mismatch");

        if (string.IsNullOrWhiteSpace(Username))
            return (false, "Username is required");

        if (string.IsNullOrWhiteSpace(FirstName))
            return (false, "First Name is required");

        if (string.IsNullOrWhiteSpace(LastName))
            return (false, "Last Name is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        return (true, null);
    }

    public User ToModel(User existingUser, Guid currentUser)
    {
        var fullName = $"{FirstName} {LastName}".Trim();
        return new User
        {
            UserId = UserId,
            Username = Username,
            FirstName = FirstName,
            LastName = LastName,
            FullName = fullName,
            Email = Email,
            PasswordHash = existingUser.PasswordHash, // Preserve existing password hash
            IsActive = IsActive,
            CreatedOn = existingUser.CreatedOn,
            CreatedBy = existingUser.CreatedBy,
            ModifiedBy = currentUser
        };
    }
}
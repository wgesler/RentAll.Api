using RentAll.Domain.Models.Users;

namespace RentAll.Api.Dtos.Users;

public class CreateUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
	public List<string> UserGroups { get; set; } = new List<string>();
    public bool IsActive { get; set; }


	public (bool IsValid, string? ErrorMessage) IsValid()
    {
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

    public User ToModel(CreateUserDto d, string passwordHash, Guid currentUser)
    {
        return new User
        {
            FirstName = d.FirstName,
            LastName = d.LastName,
            Email = d.Email,
            PasswordHash = passwordHash,
			UserGroups = d.UserGroups,
			IsActive = d.IsActive,
            CreatedBy = currentUser
        };
    }
}





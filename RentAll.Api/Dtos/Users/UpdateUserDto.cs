using RentAll.Domain.Models.Users;

namespace RentAll.Api.Dtos.Users;

public class UpdateUserDto
{
    public Guid UserId { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
	public List<string> UserGroups { get; set; } = new List<string>();
	public bool IsActive { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
    {
        if (id == Guid.Empty)
            return (false, "User ID is required");

        if (UserId != id)
            return (false, "User ID mismatch");

        if (string.IsNullOrWhiteSpace(FirstName))
            return (false, "First Name is required");

        if (string.IsNullOrWhiteSpace(LastName))
            return (false, "Last Name is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        return (true, null);
    }

    public User ToModel(UpdateUserDto d, User existingUser, Guid currentUser)
    {
        return new User
        {
            UserId = d.UserId,
            FirstName = d.FirstName,
            LastName = d.LastName,
            Email = d.Email,
            PasswordHash = existingUser.PasswordHash, // Preserve existing password hash
			UserGroups = d.UserGroups,
			IsActive = d.IsActive,
			ModifiedBy = currentUser
		};
    }
}



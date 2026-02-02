using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Users;

public class CreateUserDto
{
	public Guid OrganizationId { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
	public List<string> UserGroups { get; set; } = new List<string>();
	public List<int> OfficeAccess { get; set; } = new List<int>();
	public bool IsActive { get; set; }


	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (string.IsNullOrWhiteSpace(FirstName))
			return (false, "First Name is required");

		if (string.IsNullOrWhiteSpace(LastName))
			return (false, "Last Name is required");

		if (string.IsNullOrWhiteSpace(Email))
			return (false, "Email is required");

		if (string.IsNullOrWhiteSpace(Password))
			return (false, "Password is required");

		if (Password.Length < 8)
			return (false, "Password must be at least 8 characters");

		return (true, null);
	}

	public User ToModel(string passwordHash, Guid currentUser)
	{
		return new User
		{
			OrganizationId = OrganizationId,
			FirstName = FirstName,
			LastName = LastName,
			Email = Email,
			PasswordHash = passwordHash,
			UserGroups = UserGroups,
			OfficeAccess = OfficeAccess,
			IsActive = IsActive,
			CreatedBy = currentUser
		};
	}
}






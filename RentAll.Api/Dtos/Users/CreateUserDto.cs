using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;
using RentAll.Domain.Enums;

namespace RentAll.Api.Dtos.Users;

public class CreateUserDto
{
	public Guid OrganizationId { get; set; }
	public Guid AgentId { get; set; }
	public decimal CommissionRate { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Phone { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
	public List<string> UserGroups { get; set; } = new List<string>();
	public List<int> OfficeAccess { get; set; } = new List<int>();
	public FileDetails? FileDetails { get; set; }
	public int StartupPageId { get; set; }
	public bool IsActive { get; set; }


	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (AgentId == Guid.Empty)
			return (false, "AgentId is required");

		if (CommissionRate < 0)
			return (false, "CommissionRate must be greater than or equal to 0");

		if (string.IsNullOrWhiteSpace(FirstName))
			return (false, "First Name is required");

		if (string.IsNullOrWhiteSpace(LastName))
			return (false, "Last Name is required");

		if (string.IsNullOrWhiteSpace(Email))
			return (false, "Email is required");

		if (string.IsNullOrWhiteSpace(Phone))
			return (false, "Phone is required");

		if (string.IsNullOrWhiteSpace(Password))
			return (false, "Password is required");

		if (Password.Length < 8)
			return (false, "Password must be at least 8 characters");

		// Validate enum value
		if (!Enum.IsDefined(typeof(StartupPage), StartupPageId))
			return (false, $"Invalid StartupPage value: {StartupPageId}");

		return (true, null);
	}

	public User ToModel(string passwordHash, Guid currentUser)
	{
		return new User
		{
			OrganizationId = OrganizationId,
			AgentId = AgentId,
			CommissionRate = CommissionRate,
			FirstName = FirstName,
			LastName = LastName,
			Email = Email,
			Phone = Phone,
			PasswordHash = passwordHash,
			UserGroups = UserGroups,
			OfficeAccess = OfficeAccess,
			ProfilePath = null, // Will be set by controller after file save
			StartupPage = (StartupPage)StartupPageId,
			IsActive = IsActive,
			CreatedBy = currentUser
		};
	}
}






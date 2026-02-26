using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Users;

public class UpdateUserDto
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Password { get; set; } = string.Empty;
    public string? NewPassword { get; set; } = string.Empty;
    public List<string> UserGroups { get; set; } = new List<string>();
    public List<int> OfficeAccess { get; set; } = new List<int>();
    public string? ProfilePath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public int StartupPageId { get; set; }
    public Guid? AgentId { get; set; }
    public decimal CommissionRate { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

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

        // Validate enum value
        if (!Enum.IsDefined(typeof(StartupPage), StartupPageId))
            return (false, $"Invalid StartupPage value: {StartupPageId}");

        return (true, null);
    }

    public User ToModel(UpdateUserDto d, string passwordHash, Guid currentUser)
    {
        return new User
        {
            OrganizationId = d.OrganizationId,
            UserId = d.UserId,
            AgentId = d.AgentId,
            CommissionRate = d.CommissionRate,
            FirstName = d.FirstName,
            LastName = d.LastName,
            Email = d.Email,
            Phone = d.Phone,
            PasswordHash = passwordHash,
            UserGroups = d.UserGroups,
            OfficeAccess = d.OfficeAccess,
            ProfilePath = d.ProfilePath,
            StartupPage = (StartupPage)d.StartupPageId,
            IsActive = d.IsActive,
            ModifiedBy = currentUser
        };
    }
}






using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Users;

public class UserResponseDto
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public Guid? AgentId { get; set; }
    public decimal CommissionRate { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<string> UserGroups { get; set; } = new List<string>();
    public List<int> OfficeAccess { get; set; } = new List<int>();
    public string? ProfilePath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public int StartupPageId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public UserResponseDto(User user)
    {
        UserId = user.UserId;
        OrganizationId = user.OrganizationId;
        OrganizationName = user.OrganizationName;
        AgentId = user.AgentId;
        CommissionRate = user.CommissionRate;
        FirstName = user.FirstName;
        LastName = user.LastName;
        Email = user.Email;
        Phone = user.Phone;
        UserGroups = user.UserGroups;
        OfficeAccess = user.OfficeAccess;
        ProfilePath = user.ProfilePath;
        FileDetails = user.FileDetails;
        StartupPageId = (int)user.StartupPage;
        IsActive = user.IsActive;
        CreatedOn = user.CreatedOn;
        CreatedBy = user.CreatedBy;
        ModifiedOn = user.ModifiedOn;
        ModifiedBy = user.ModifiedBy;
    }
}






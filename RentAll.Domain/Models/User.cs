using RentAll.Domain.Models.Common;
using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class User
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
	public string? OrganizationName { get; set; }
	public string Username { get; set; } = string.Empty;
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public Guid AgentId { get; set; }
	public string PasswordHash { get; set; } = string.Empty;
	public List<string> UserGroups { get; set; } = new List<string>();
	public List<int> OfficeAccess { get; set; } = new List<int>();
	public string? ProfilePath { get; set; }
	public FileDetails? FileDetails { get; set; }
	public StartupPage StartupPage { get; set; }
	public bool IsActive { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }
}
namespace RentAll.Infrastructure.Entities;

public class UserEntity
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
	public string UserGroups { get; set; } = string.Empty;
	public string OfficeAccess { get; set; } = string.Empty;
	public string? ProfilePath { get; set; }
	public bool IsActive { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

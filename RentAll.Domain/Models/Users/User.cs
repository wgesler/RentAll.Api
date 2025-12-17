namespace RentAll.Domain.Models.Users;

public class User
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
	public string Username { get; set; } = string.Empty;
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string PasswordHash { get; set; } = string.Empty;
	public List<string> UserGroups { get; set; } = new List<string>();
	public bool IsActive { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }
}
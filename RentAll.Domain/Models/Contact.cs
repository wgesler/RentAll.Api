using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class Contact
{
    public Guid ContactId { get; set; }
    public Guid OrganizationId { get; set; }
    public string ContactCode { get; set; } = string.Empty;
    public EntityType EntityType { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }
}



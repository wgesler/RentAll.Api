namespace RentAll.Infrastructure.Entities.Contacts;

public class ContactEntity
{
    public Guid ContactId { get; set; }
    public Guid OrganizationId { get; set; }
    public string ContactCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public int EntityTypeId { get; set; }
    public Guid? EntityId { get; set; }
    public int? OwnerTypeId { get; set; }
    public string? CompanyName { get; set; }
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Phone { get; set; } 
    public string Email { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public bool IsInternational { get; set; }
    public string? W9Path { get; set; }
    public DateTimeOffset? W9Expiration { get; set; }
    public string? InsurancePath { get; set; }
    public DateTimeOffset? InsuranceExpiration { get; set; }
    public int Markup { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

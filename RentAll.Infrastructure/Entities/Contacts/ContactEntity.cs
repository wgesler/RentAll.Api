namespace RentAll.Infrastructure.Entities.Contacts;

public class ContactEntity
{
    public Guid ContactId { get; set; }
    public Guid OrganizationId { get; set; }
    public string ContactCode { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string OfficeAccess { get; set; } = string.Empty;
    public int EntityTypeId { get; set; }
    public int? OwnerTypeId { get; set; }
    public int? VendorTypeId { get; set; }
    public string Properties { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? CompanyEmail { get; set; }
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
    public string? InsurancePath { get; set; }
    public DateTimeOffset? InsuranceExpiration { get; set; }
    public int? Markup { get; set; }
    public decimal? RevenueSplitOwner { get; set; }
    public decimal? RevenueSplitOffice { get; set; }
    public decimal? WorkingCapitalBalance { get; set; }
    public decimal? LinenAndTowelFee { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    public string? AccountNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

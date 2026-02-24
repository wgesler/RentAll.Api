namespace RentAll.Infrastructure.Entities.Organizations;

public class OrganizationEntity
{
    public Guid OrganizationId { get; set; }
    public string OrganizationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Fax { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? Website { get; set; }
    public string? LogoPath { get; set; }
    public bool IsInternational { get; set; }
    public int CurrentInvoiceNo { get; set; }
    public decimal OfficeFee { get; set; }
    public decimal UserFee { get; set; }
    public decimal Unit50Fee { get; set; }
    public decimal Unit100Fee { get; set; }
    public decimal Unit200Fee { get; set; }
    public decimal Unit500Fee { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}





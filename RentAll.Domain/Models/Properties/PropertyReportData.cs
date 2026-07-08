using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class PropertyReportData
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public string PropertyTypeDescription { get; set; } = string.Empty;
    public PropertyLeaseType PropertyLeaseType { get; set; }
    public Guid? PrimaryOwnerId { get; set; }
    public OwnerType? OwnerType { get; set; }
    public string? CompanyName { get; set; }
    public string OwnerNames { get; set; } = string.Empty;
    public string OwnerNameLine { get; set; } = string.Empty;
    public decimal WorkingCapitalBalance { get; set; }
    public ManagementFeeType ManagementFeeType { get; set; }
    public decimal RevenueSplitOwner { get; set; }
    public decimal RevenueSplitOffice { get; set; }
}

namespace RentAll.Infrastructure.Entities.Properties;

public class PropertyReportDataEntity
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public int PropertyTypeId { get; set; }
    public string PropertyType { get; set; } = string.Empty;
    public int PropertyLeaseTypeId { get; set; }
    public Guid? PrimaryOwnerId { get; set; }
    public int? OwnerTypeId { get; set; }
    public string? CompanyName { get; set; }
    public string OwnerNames { get; set; } = string.Empty;
    public string OwnerNameLine { get; set; } = string.Empty;
    public decimal WorkingCapitalBalance { get; set; }
    public int ManagementFeeTypeId { get; set; }
    public decimal RevenueSplitOwner { get; set; }
    public decimal RevenueSplitOffice { get; set; }
}

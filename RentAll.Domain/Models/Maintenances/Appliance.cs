namespace RentAll.Domain.Models;

public class Appliance
{
    public Guid ApplianceId { get; set; }
    public Guid PropertyId { get; set; }
    public string ApplianceName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelNo { get; set; } = string.Empty;
    public string SerialNo { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

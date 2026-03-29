namespace RentAll.Api.Dtos.Maintenances.Appliances;

public class ApplianceResponseDto
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

    public ApplianceResponseDto(Appliance appliance)
    {
        ApplianceId = appliance.ApplianceId;
        PropertyId = appliance.PropertyId;
        ApplianceName = appliance.ApplianceName;
        Manufacturer = appliance.Manufacturer;
        ModelNo = appliance.ModelNo;
        SerialNo = appliance.SerialNo;
        IsActive = appliance.IsActive;
        CreatedOn = appliance.CreatedOn;
        CreatedBy = appliance.CreatedBy;
        ModifiedOn = appliance.ModifiedOn;
        ModifiedBy = appliance.ModifiedBy;
    }
}

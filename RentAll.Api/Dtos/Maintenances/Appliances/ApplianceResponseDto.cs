namespace RentAll.Api.Dtos.Maintenances.Appliances;

public class ApplianceResponseDto
{
    public int ApplianceId { get; set; }
    public Guid PropertyId { get; set; }
    public string ApplianceName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelNo { get; set; } = string.Empty;
    public string SerialNo { get; set; } = string.Empty;

    public ApplianceResponseDto(Appliance appliance)
    {
        ApplianceId = appliance.ApplianceId;
        PropertyId = appliance.PropertyId;
        ApplianceName = appliance.ApplianceName;
        Manufacturer = appliance.Manufacturer;
        ModelNo = appliance.ModelNo;
        SerialNo = appliance.SerialNo;
    }
}

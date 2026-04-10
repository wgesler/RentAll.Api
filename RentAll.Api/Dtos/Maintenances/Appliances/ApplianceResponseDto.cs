using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.Appliances;

public class ApplianceResponseDto
{
    public int ApplianceId { get; set; }
    public Guid PropertyId { get; set; }
    public string ApplianceName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string? ModelNo { get; set; }
    public string? SerialNo { get; set; }
    public string? DecalPath { get; set; }
    public FileDetails? DecalFileDetails { get; set; }

    public ApplianceResponseDto(Appliance appliance)
    {
        ApplianceId = appliance.ApplianceId;
        PropertyId = appliance.PropertyId;
        ApplianceName = appliance.ApplianceName;
        Manufacturer = appliance.Manufacturer;
        ModelNo = appliance.ModelNo;
        SerialNo = appliance.SerialNo;
        DecalPath = appliance.DecalPath;
    }
}

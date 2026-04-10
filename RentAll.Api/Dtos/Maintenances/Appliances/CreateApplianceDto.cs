using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.Appliances;

public class CreateApplianceDto
{
    public Guid PropertyId { get; set; }
    public string ApplianceName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string? ModelNo { get; set; }
    public string? SerialNo { get; set; }
    public string? DecalPath { get; set; }
    public FileDetails? DecalFileDetails { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (string.IsNullOrWhiteSpace(ApplianceName))
            return (false, "ApplianceName is required");

        if (string.IsNullOrWhiteSpace(Manufacturer))
            return (false, "Manufacturer is required");

        return (true, null);
    }

    public Appliance ToModel()
    {
        return new Appliance
        {
            PropertyId = PropertyId,
            ApplianceName = ApplianceName,
            Manufacturer = Manufacturer,
            ModelNo = ModelNo,
            SerialNo = SerialNo,
            DecalPath = DecalPath
        };
    }
}

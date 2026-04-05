namespace RentAll.Api.Dtos.Maintenances.Appliances;

public class CreateApplianceDto
{
    public Guid PropertyId { get; set; }
    public string ApplianceName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelNo { get; set; } = string.Empty;
    public string SerialNo { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (string.IsNullOrWhiteSpace(ApplianceName))
            return (false, "ApplianceName is required");

        if (string.IsNullOrWhiteSpace(Manufacturer))
            return (false, "Manufacturer is required");

        if (string.IsNullOrWhiteSpace(ModelNo))
            return (false, "ModelNo is required");

        if (string.IsNullOrWhiteSpace(SerialNo))
            return (false, "SerialNo is required");

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
            SerialNo = SerialNo
        };
    }
}

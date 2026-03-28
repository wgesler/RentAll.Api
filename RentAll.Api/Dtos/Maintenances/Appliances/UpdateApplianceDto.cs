namespace RentAll.Api.Dtos.Maintenances.Appliances;

public class UpdateApplianceDto
{
    public Guid ApplianceId { get; set; }
    public Guid MaintenanceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ApplianceId == Guid.Empty)
            return (false, "ApplianceId is required");

        if (MaintenanceId == Guid.Empty)
            return (false, "MaintenanceId is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (string.IsNullOrWhiteSpace(Make))
            return (false, "Make is required");

        if (string.IsNullOrWhiteSpace(Model))
            return (false, "Model is required");

        return (true, null);
    }

    public Appliance ToModel(Guid currentUser)
    {
        return new Appliance
        {
            ApplianceId = ApplianceId,
            MaintenanceId = MaintenanceId,
            Name = Name,
            Make = Make,
            Model = Model,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}

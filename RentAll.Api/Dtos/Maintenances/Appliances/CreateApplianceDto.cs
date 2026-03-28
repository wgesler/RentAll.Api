namespace RentAll.Api.Dtos.Maintenances.Appliances;

public class CreateApplianceDto
{
    public Guid MaintenanceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
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
            MaintenanceId = MaintenanceId,
            Name = Name,
            Make = Make,
            Model = Model,
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }
}

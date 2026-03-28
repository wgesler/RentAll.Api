namespace RentAll.Api.Dtos.Maintenances.Appliances;

public class ApplianceResponseDto
{
    public Guid ApplianceId { get; set; }
    public Guid MaintenanceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public ApplianceResponseDto(Appliance appliance)
    {
        ApplianceId = appliance.ApplianceId;
        MaintenanceId = appliance.MaintenanceId;
        Name = appliance.Name;
        Make = appliance.Make;
        Model = appliance.Model;
        IsActive = appliance.IsActive;
        CreatedOn = appliance.CreatedOn;
        CreatedBy = appliance.CreatedBy;
        ModifiedOn = appliance.ModifiedOn;
        ModifiedBy = appliance.ModifiedBy;
    }
}

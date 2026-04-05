namespace RentAll.Api.Dtos.Maintenances.MaintenanceItems;

public class MaintenanceItemResponseDto
{
    public int MaintenanceItemId { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int MonthsBetweenService { get; set; }
    public DateTimeOffset? LastServicedOn { get; set; }

    public MaintenanceItemResponseDto(MaintenanceItem maintenanceItem)
    {
        MaintenanceItemId = maintenanceItem.MaintenanceItemId;
        PropertyId = maintenanceItem.PropertyId;
        Name = maintenanceItem.Name;
        Notes = maintenanceItem.Notes;
        MonthsBetweenService = maintenanceItem.MonthsBetweenService;
        LastServicedOn = maintenanceItem.LastServicedOn;
    }
}

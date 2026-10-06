namespace RentAll.Api.Dtos.Maintenances.MaintenanceItems;

public class MaintenanceItemListResponseDto
{
    public int MaintenanceItemId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int MonthsBetweenService { get; set; }
    public DateTimeOffset? LastServicedOn { get; set; }

    public MaintenanceItemListResponseDto(MaintenanceItemList maintenanceItem)
    {
        MaintenanceItemId = maintenanceItem.MaintenanceItemId;
        PropertyId = maintenanceItem.PropertyId;
        PropertyCode = maintenanceItem.PropertyCode;
        OfficeId = maintenanceItem.OfficeId;
        Name = maintenanceItem.Name;
        Notes = maintenanceItem.Notes;
        MonthsBetweenService = maintenanceItem.MonthsBetweenService;
        LastServicedOn = maintenanceItem.LastServicedOn;
    }
}

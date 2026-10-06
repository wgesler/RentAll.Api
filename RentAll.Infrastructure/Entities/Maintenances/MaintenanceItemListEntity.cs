namespace RentAll.Infrastructure.Entities.Maintenances;

public class MaintenanceItemListEntity
{
    public int MaintenanceItemId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int MonthsBetweenService { get; set; }
    public DateTimeOffset? LastServicedOn { get; set; }
}

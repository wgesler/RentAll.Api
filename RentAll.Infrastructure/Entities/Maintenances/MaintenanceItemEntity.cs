namespace RentAll.Infrastructure.Entities.Maintenances;

public class MaintenanceItemEntity
{
    public int MaintenanceItemId { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int MonthsBetweenService { get; set; }
    public DateTimeOffset? LastServicedOn { get; set; }
}

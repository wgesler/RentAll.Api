namespace RentAll.Domain.Models;

public class MaintenanceItem
{
    public int MaintenanceItemId { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int MonthsBetweenService { get; set; } = 12;
    public DateTimeOffset? LastServicedOn { get; set; }
}

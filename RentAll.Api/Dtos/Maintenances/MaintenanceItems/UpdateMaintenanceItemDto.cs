namespace RentAll.Api.Dtos.Maintenances.MaintenanceItems;

public class UpdateMaintenanceItemDto
{
    public int MaintenanceItemId { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int MonthsBetweenService { get; set; }
    public DateTimeOffset? LastServicedOn { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (MaintenanceItemId <= 0)
            return (false, "MaintenanceItemId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (MonthsBetweenService <= 0)
            return (false, "MonthsBetweenService must be greater than zero");

        return (true, null);
    }

    public MaintenanceItem ToModel()
    {
        return new MaintenanceItem
        {
            MaintenanceItemId = MaintenanceItemId,
            PropertyId = PropertyId,
            Name = Name,
            Notes = Notes,
            MonthsBetweenService = MonthsBetweenService,
            LastServicedOn = LastServicedOn
        };
    }
}

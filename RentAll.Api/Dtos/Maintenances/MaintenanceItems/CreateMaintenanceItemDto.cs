namespace RentAll.Api.Dtos.Maintenances.MaintenanceItems;

public class CreateMaintenanceItemDto
{
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int MonthsBetweenService { get; set; } = 12;
    public DateTimeOffset? LastServicedOn { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
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
            PropertyId = PropertyId,
            Name = Name,
            Notes = Notes,
            MonthsBetweenService = MonthsBetweenService,
            LastServicedOn = LastServicedOn
        };
    }
}

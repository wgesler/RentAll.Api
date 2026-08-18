namespace RentAll.Infrastructure.Entities.Organizations;

public class UserGuideEntity
{
    public Guid UserGuideId { get; set; }
    public string Welcome { get; set; } = string.Empty;
    public string Dashboard { get; set; } = string.Empty;
    public string DashboardStaff { get; set; } = string.Empty;
    public string DashboardOwner { get; set; } = string.Empty;
    public string Leads { get; set; } = string.Empty;
    public string Boards { get; set; } = string.Empty;
    public string Reservations { get; set; } = string.Empty;
    public string Properties { get; set; } = string.Empty;
    public string Tickets { get; set; } = string.Empty;
    public string Maintenance { get; set; } = string.Empty;
    public string Accounting { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Emails { get; set; } = string.Empty;
    public string Documents { get; set; } = string.Empty;
    public string Contacts { get; set; } = string.Empty;
    public string Users { get; set; } = string.Empty;
    public string Settings { get; set; } = string.Empty;
    public string Logs { get; set; } = string.Empty;
    public string Organizations { get; set; } = string.Empty;
    public string Billing { get; set; } = string.Empty;
}

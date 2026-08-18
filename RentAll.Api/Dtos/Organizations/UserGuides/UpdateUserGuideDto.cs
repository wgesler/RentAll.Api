namespace RentAll.Api.Dtos.Organizations.UserGuides;

public class UpdateUserGuideDto
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

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        return (true, null);
    }

    public UserGuide ToModel()
    {
        return new UserGuide
        {
            UserGuideId = UserGuideId,
            Welcome = Welcome ?? string.Empty,
            Dashboard = Dashboard ?? string.Empty,
            DashboardStaff = DashboardStaff ?? string.Empty,
            DashboardOwner = DashboardOwner ?? string.Empty,
            Leads = Leads ?? string.Empty,
            Boards = Boards ?? string.Empty,
            Reservations = Reservations ?? string.Empty,
            Properties = Properties ?? string.Empty,
            Tickets = Tickets ?? string.Empty,
            Maintenance = Maintenance ?? string.Empty,
            Accounting = Accounting ?? string.Empty,
            Owner = Owner ?? string.Empty,
            Emails = Emails ?? string.Empty,
            Documents = Documents ?? string.Empty,
            Contacts = Contacts ?? string.Empty,
            Users = Users ?? string.Empty,
            Settings = Settings ?? string.Empty,
            Logs = Logs ?? string.Empty,
            Organizations = Organizations ?? string.Empty,
            Billing = Billing ?? string.Empty
        };
    }
}

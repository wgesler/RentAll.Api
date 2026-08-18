namespace RentAll.Api.Dtos.Organizations.UserGuides;

public class UserGuideResponseDto
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

    public UserGuideResponseDto(UserGuide userGuide)
    {
        UserGuideId = userGuide.UserGuideId;
        Welcome = userGuide.Welcome;
        Dashboard = userGuide.Dashboard;
        DashboardStaff = userGuide.DashboardStaff;
        DashboardOwner = userGuide.DashboardOwner;
        Leads = userGuide.Leads;
        Boards = userGuide.Boards;
        Reservations = userGuide.Reservations;
        Properties = userGuide.Properties;
        Tickets = userGuide.Tickets;
        Maintenance = userGuide.Maintenance;
        Accounting = userGuide.Accounting;
        Owner = userGuide.Owner;
        Emails = userGuide.Emails;
        Documents = userGuide.Documents;
        Contacts = userGuide.Contacts;
        Users = userGuide.Users;
        Settings = userGuide.Settings;
        Logs = userGuide.Logs;
        Organizations = userGuide.Organizations;
        Billing = userGuide.Billing;
    }
}

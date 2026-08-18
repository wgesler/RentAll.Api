using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Selects
    public async Task<UserGuide?> GetUserGuideAsync()
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<UserGuideEntity>("Organization.UserGuide_Get", null);
        if (res == null || !res.Any())
            return new UserGuide();

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
    public async Task<UserGuide> UpsertUserGuideAsync(UserGuide userGuide, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<UserGuideEntity>("Organization.UserGuide_Upsert", new
        {
            UserGuideId = userGuide.UserGuideId,
            Welcome = userGuide.Welcome,
            Dashboard = userGuide.Dashboard,
            DashboardStaff = userGuide.DashboardStaff,
            DashboardOwner = userGuide.DashboardOwner,
            Leads = userGuide.Leads,
            Boards = userGuide.Boards,
            Reservations = userGuide.Reservations,
            Properties = userGuide.Properties,
            Tickets = userGuide.Tickets,
            Maintenance = userGuide.Maintenance,
            Accounting = userGuide.Accounting,
            Owner = userGuide.Owner,
            Emails = userGuide.Emails,
            Documents = userGuide.Documents,
            Contacts = userGuide.Contacts,
            Users = userGuide.Users,
            Settings = userGuide.Settings,
            Logs = userGuide.Logs,
            Organizations = userGuide.Organizations,
            Billing = userGuide.Billing,
            ModifiedBy = modifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("UserGuide not updated");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion
}

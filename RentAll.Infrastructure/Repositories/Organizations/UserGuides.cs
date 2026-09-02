using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    private static readonly (string TopicKey, Func<UserGuideEntity, string> GetLegacyHtml)[] UserGuideLegacySectionFallbacks =
    [
        ("welcome", e => e.Welcome),
        ("dashboard", e => e.Dashboard),
        ("dashboard-staff", e => e.DashboardStaff),
        ("dashboard-owner", e => e.DashboardOwner),
        ("leads", e => e.Leads),
        ("boards", e => e.Boards),
        ("reservations", e => e.Reservations),
        ("properties", e => e.Properties),
        ("tickets", e => e.Tickets),
        ("maintenance", e => e.Maintenance),
        ("accounting", e => e.Accounting),
        ("owner", e => e.Owner),
        ("emails", e => e.Emails),
        ("documents", e => e.Documents),
        ("contacts", e => e.Contacts),
        ("users", e => e.Users),
        ("settings", e => e.Settings),
        ("logs", e => e.Logs),
        ("organizations", e => e.Organizations),
        ("billing", e => e.Billing)
    ];

    #region Selects
    public async Task<UserGuide?> GetUserGuideAsync()
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, sections) = await db.DapperProcQueryMultipleAsync<UserGuideEntity, UserGuideSectionEntity>("Organization.UserGuide_Get", null);
        var header = headers.FirstOrDefault();
        if (header == null)
            return new UserGuide();

        return ConvertEntityToModel(header, sections);
    }
    #endregion

    #region Updates
    public async Task<UserGuide> UpsertUserGuideAsync(UserGuide userGuide, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, sections) = await db.DapperProcQueryMultipleAsync<UserGuideEntity, UserGuideSectionEntity>("Organization.UserGuide_Upsert", new
        {
            UserGuideId = userGuide.UserGuideId,
            SectionsJson = JsonSerializer.Serialize(userGuide.Sections ?? new Dictionary<string, string>()),
            ModifiedBy = modifiedBy
        });

        var header = headers.FirstOrDefault();
        if (header == null)
            throw new Exception("UserGuide not updated");

        return ConvertEntityToModel(header, sections);
    }
    #endregion

    private UserGuide ConvertEntityToModel(UserGuideEntity header, IEnumerable<UserGuideSectionEntity> sections)
    {
        var sectionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var section in sections ?? [])
        {
            if (string.IsNullOrWhiteSpace(section.TopicKey))
                continue;

            sectionMap[section.TopicKey] = section.Html ?? string.Empty;
        }

        foreach (var (topicKey, getLegacyHtml) in UserGuideLegacySectionFallbacks)
        {
            if (!sectionMap.ContainsKey(topicKey))
                sectionMap[topicKey] = getLegacyHtml(header) ?? string.Empty;
        }

        return new UserGuide
        {
            UserGuideId = header.UserGuideId,
            Sections = sectionMap
        };
    }
}

using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Selects
    public async Task<Branding?> GetBrandingByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BrandingEntity>("Organization.Branding_GetByOrganizationId", new
        {
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
    public async Task<Branding> UpsertBrandingByOrganizationIdAsync(Branding branding, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BrandingEntity>("Organization.Branding_UpsertByOrganizationId", new
        {
            OrganizationId = branding.OrganizationId,
            PrimaryColor = branding.PrimaryColor,
            AccentColor = branding.AccentColor,
            HeaderBackgroundColor = branding.HeaderBackgroundColor,
            HeaderTextColor = branding.HeaderTextColor,
            LogoPath = branding.LogoPath,
            CollapsedLogoPath = branding.CollapsedLogoPath,
            ModifiedBy = modifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Branding not updated");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion
}

using Microsoft.Data.SqlClient;
using RentAll.Domain.Models.Leads;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Leads;

namespace RentAll.Infrastructure.Repositories.Leads;

public partial class LeadRepository
{
    #region Selects

    public async Task<OwnerHtml?> GetOwnerHtmlByPropertyIdAsync(Guid propertyId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerHtmlEntity>("Lead.OwnerHtml_GetByPropertyId", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertOwnerHtmlEntityToModel(res.First());
    }

    #endregion
}

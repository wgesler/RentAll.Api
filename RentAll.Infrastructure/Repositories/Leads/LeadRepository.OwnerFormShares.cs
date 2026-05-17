using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Leads;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Leads;

namespace RentAll.Infrastructure.Repositories.Leads;

public partial class LeadRepository : ILeadRepository
{
    #region Selects

    public async Task<LeadOwnerFormShare?> GetOwnerFormShareByTokenHashAsync(string tokenHash)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerFormShareEntity>("Lead.OwnerFormShare_GetByTokenHash", new
        {
            TokenHash = tokenHash,
            NowUtc = DateTimeOffset.UtcNow
        });

        if (res == null || !res.Any())
            return null;

        return ConvertOwnerFormShareEntityToModel(res.First());
    }

    #endregion

    #region Creates

    public async Task<LeadOwnerFormShare> UpsertOwnerFormShareByOwnerIdAsync(LeadOwnerFormShare share)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerFormShareEntity>("Lead.OwnerFormShare_UpsertByOwnerId", new
        {
            OwnerId = share.OwnerId,
            ShareId = share.ShareId,
            TokenHash = share.TokenHash,
            ExpiresOn = share.ExpiresOn
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Owner form share was not created.");

        return ConvertOwnerFormShareEntityToModel(res.First());
    }

    #endregion

    #region Deletes

    public async Task DeleteExpiredOwnerFormSharesAsync()
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Lead.OwnerFormShare_DeleteExpired", new
        {
            NowUtc = DateTimeOffset.UtcNow
        });
    }

    #endregion
}

using Microsoft.Data.SqlClient;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Maintenances;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region ReceiptMatch
    public async Task<IEnumerable<Domain.Models.ReceiptMatch>> GetReceiptMatchesByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<ReceiptMatchEntity>("Maintenance.ReceiptMatch_GetByOrganizationId", new { OrganizationId = organizationId });
        return (rows ?? []).Select(row => row.ToModel());
    }

    public async Task<Domain.Models.ReceiptMatch> CreateReceiptMatchAsync(Domain.Models.ReceiptMatch match)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<ReceiptMatchEntity>("Maintenance.ReceiptMatch_Add", new
        {
            OrganizationId = match.OrganizationId,
            SourceName = match.SourceName.Trim(),
            MatchedId = match.MatchedId,
            MatchedName = match.MatchedName,
            IsActive = match.IsActive
        });
        return (rows ?? []).First().ToModel();
    }

    public async Task<Domain.Models.ReceiptMatch> UpdateReceiptMatchAsync(Domain.Models.ReceiptMatch match)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<ReceiptMatchEntity>("Maintenance.ReceiptMatch_UpdateById", new
        {
            ReceiptMatchId = match.ReceiptMatchId,
            OrganizationId = match.OrganizationId,
            SourceName = match.SourceName.Trim(),
            MatchedId = match.MatchedId,
            MatchedName = match.MatchedName,
            IsActive = match.IsActive
        });
        return (rows ?? []).First().ToModel();
    }

    public async Task DeleteReceiptMatchByIdAsync(Guid receiptMatchId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.ReceiptMatch_DeleteById", new { ReceiptMatchId = receiptMatchId, OrganizationId = organizationId });
    }
    #endregion
}

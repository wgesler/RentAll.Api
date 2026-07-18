using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    public async Task<ReconcileDraft?> GetReconcileDraftByAccountIdAsync(Guid organizationId, int officeId, int accountId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReconcileDraftEntity>("Accounting.ReconcileDraft_GetByAccountId", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            AccountId = accountId
        });

        var entity = res?.FirstOrDefault();
        return entity == null ? null : ConvertReconcileDraftEntityToModel(entity);
    }

    public async Task<ReconcileDraft> UpsertReconcileDraftAsync(ReconcileDraft reconcileDraft)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReconcileDraftEntity>("Accounting.ReconcileDraft_Upsert", new
        {
            OrganizationId = reconcileDraft.OrganizationId,
            OfficeId = reconcileDraft.OfficeId,
            AccountId = reconcileDraft.AccountId,
            StatementDate = reconcileDraft.StatementDate,
            EndingBalance = reconcileDraft.EndingBalance,
            ServiceChargeAmount = reconcileDraft.ServiceChargeAmount,
            ServiceChargeDate = reconcileDraft.ServiceChargeDate,
            ServiceChargeAccountId = reconcileDraft.ServiceChargeAccountId,
            InterestAmount = reconcileDraft.InterestAmount,
            InterestDate = reconcileDraft.InterestDate,
            InterestAccountId = reconcileDraft.InterestAccountId
        });

        if (res == null || !res.Any())
            throw new Exception("Reconcile draft not saved");

        return ConvertReconcileDraftEntityToModel(res.First());
    }

    public async Task DeleteReconcileDraftByAccountIdAsync(Guid organizationId, int officeId, int accountId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.ReconcileDraft_DeleteByAccountId", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            AccountId = accountId
        });
    }

    private static ReconcileDraft ConvertReconcileDraftEntityToModel(ReconcileDraftEntity entity)
    {
        return new ReconcileDraft
        {
            AccountId = entity.AccountId,
            OrganizationId = entity.OrganizationId,
            OfficeId = entity.OfficeId,
            StatementDate = entity.StatementDate,
            EndingBalance = entity.EndingBalance,
            ServiceChargeAmount = entity.ServiceChargeAmount,
            ServiceChargeDate = entity.ServiceChargeDate,
            ServiceChargeAccountId = entity.ServiceChargeAccountId,
            InterestAmount = entity.InterestAmount,
            InterestDate = entity.InterestDate,
            InterestAccountId = entity.InterestAccountId
        };
    }
}

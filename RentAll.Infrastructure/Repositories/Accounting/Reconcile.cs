using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    public async Task<List<Reconcile>> GetReconcilesByAccountIdAsync(Guid organizationId, int officeId, int accountId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReconcileEntity>("Accounting.Reconcile_GetAllByAccountId", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            AccountId = accountId
        });

        if (res == null || !res.Any())
            return new List<Reconcile>();

        return res.Select(ConvertReconcileEntityToModel).ToList();
    }

    public async Task<Reconcile?> GetReconcileByIdAsync(int reconcileId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReconcileEntity>("Accounting.Reconcile_GetById", new
        {
            ReconcileId = reconcileId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        var entity = res?.FirstOrDefault();
        return entity == null ? null : ConvertReconcileEntityToModel(entity);
    }

    public async Task<Reconcile> CreateReconcileAsync(Reconcile reconcile)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReconcileEntity>("Accounting.Reconcile_Add", new
        {
            OrganizationId = reconcile.OrganizationId,
            OfficeId = reconcile.OfficeId,
            AccountId = reconcile.AccountId,
            StatementDate = reconcile.StatementDate,
            EndingBalance = reconcile.EndingBalance,
            ServiceChargeAmount = reconcile.ServiceChargeAmount,
            ServiceChargeDate = reconcile.ServiceChargeDate,
            ServiceChargeAccountId = reconcile.ServiceChargeAccountId,
            InterestAmount = reconcile.InterestAmount,
            InterestDate = reconcile.InterestDate,
            InterestAccountId = reconcile.InterestAccountId
        });

        if (res == null || !res.Any())
            throw new Exception("Reconcile not created");

        return ConvertReconcileEntityToModel(res.First());
    }

    public async Task<Reconcile> UpdateReconcileByIdAsync(Reconcile reconcile)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReconcileEntity>("Accounting.Reconcile_UpdateById", new
        {
            ReconcileId = reconcile.ReconcileId,
            OrganizationId = reconcile.OrganizationId,
            OfficeId = reconcile.OfficeId,
            AccountId = reconcile.AccountId,
            StatementDate = reconcile.StatementDate,
            EndingBalance = reconcile.EndingBalance,
            ServiceChargeAmount = reconcile.ServiceChargeAmount,
            ServiceChargeDate = reconcile.ServiceChargeDate,
            ServiceChargeAccountId = reconcile.ServiceChargeAccountId,
            InterestAmount = reconcile.InterestAmount,
            InterestDate = reconcile.InterestDate,
            InterestAccountId = reconcile.InterestAccountId
        });

        if (res == null || !res.Any())
            throw new Exception("Reconcile not found");

        return ConvertReconcileEntityToModel(res.First());
    }

    public async Task DeleteReconcileByIdAsync(int reconcileId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.Reconcile_DeleteById", new
        {
            ReconcileId = reconcileId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });
    }

    private static Reconcile ConvertReconcileEntityToModel(ReconcileEntity entity)
    {
        return new Reconcile
        {
            ReconcileId = entity.ReconcileId,
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

using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Reconcile
    public async Task<Reconcile?> RemoveLastReconcileAsync(Guid organizationId, int officeId, int accountId)
    {
        var reconciles = await _accountingRepository.GetReconcilesByAccountIdAsync(organizationId, officeId, accountId);
        var lastReconcile = reconciles.FirstOrDefault();
        if (lastReconcile == null)
            throw new InvalidOperationException("No completed reconciliation was found for this account.");

        await _accountingRepository.DeleteReconcileByIdAsync(lastReconcile.ReconcileId, organizationId, officeId);

        var latestReconcile = await GetLatestReconciliationAsync(organizationId, officeId, accountId);
        if (latestReconcile?.StatementDate is DateOnly statementDate && latestReconcile.EndingBalance is decimal endingBalance)
        {
            await _accountingRepository.UpdateChartOfAccountReconcileByIdAsync(organizationId, officeId, accountId, endingBalance, statementDate);
            return latestReconcile;
        }

        var chartOfAccount = await _accountingRepository.GetChartOfAccountByIdAsync(organizationId, officeId, accountId);
        if (chartOfAccount == null)
            throw new InvalidOperationException("Chart of account not found.");

        chartOfAccount.EndingBalance = null;
        chartOfAccount.StatementDate = null;
        await _accountingRepository.UpdateChartOfAccountByIdAsync(chartOfAccount);
        return null;
    }

    public async Task<Reconcile?> GetLatestReconciliationAsync(Guid organizationId, int officeId, int accountId)
    {
        var reconciles = await _accountingRepository.GetReconcilesByAccountIdAsync(organizationId, officeId, accountId);
        return reconciles.FirstOrDefault();
    }
    #endregion
}

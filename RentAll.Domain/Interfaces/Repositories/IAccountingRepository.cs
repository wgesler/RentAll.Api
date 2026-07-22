using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IAccountingRepository
{
    #region Invoices
    Task<IEnumerable<Invoice>> GetInvoicesAsync(InvoiceGetCriteria criteria);
    Task<IEnumerable<Invoice>> GetActiveInvoicesByAccountingMonthAsync(ActiveInvoiceByAccountingMonthCriteria criteria);
    Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId, Guid organizationId);

    Task<Invoice> CreateAsync(Invoice invoice);
    Task<Invoice> UpdateByIdAsync(Invoice invoice);
    Task<IReadOnlyList<Invoice>> UpdateByIdsInTransactionAsync(IReadOnlyList<Invoice> invoices);
    Task<int> DeactivateInvoicesByReservationIdAsync(Guid organizationId, Guid reservationId, Guid modifiedBy);
    Task<int> ReactivateInvoicesByReservationIdAsync(Guid organizationId, Guid reservationId, Guid modifiedBy);
    Task DeleteInvoiceByIdAsync(Guid invoiceId, Guid organizationId);
    #endregion

    #region CostCodes
    Task<List<CostCode>> GetCostCodesByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<List<CostCode>> GetCostCodesByOfficeIdAsync(Guid organizationId, int officeId);
    Task<CostCode?> GetCostCodeByIdAsync(int costCodeId, Guid organizationId, int officeId);
    Task<CostCode?> GetByCostCodeAsync(string costCode, Guid organizationId, int officeId);
    Task<CostCode?> GetByDescriptionAsync(string description, Guid organizationId, int officeId);
    Task<bool> ExistsByCostCodeAsync(string costCode, Guid organizationI, int officeId);

    Task<CostCode> CreateAsync(CostCode costCode);
    Task<CostCode> UpdateByIdAsync(CostCode costCode);
    Task DeleteCostCodeByIdAsync(int costCodeId, Guid organizationId, int officeId);
    #endregion

    #region ClosedDate
    Task<List<ClosedDate>> GetClosedDateByCriteriaAsync(Guid organizationId, string officeIds, DateOnly? startDate, DateOnly? endDate, int? postingStatusId);
    Task<ClosedDate?> GetClosedDateByIdAsync(int closedDateId, Guid organizationId, int officeId);
    Task<ClosedDate> CreateClosedDateAsync(ClosedDate closedDate);
    Task<ClosedDate> UpdateClosedDateByIdAsync(ClosedDate closedDate);
    Task DeleteClosedDateByIdAsync(int closedDateId, Guid organizationId, int officeId);
    Task DeleteSoftClosedDatesOverlappingRangeAsync(Guid organizationId, int officeId, DateOnly startDate, DateOnly endDate, int? excludeClosedDateId = null);
    Task<PostingStatus> CheckAccountingPeriodAsync(Guid organizationId, int officeId, DateOnly accountingPeriod);
    #endregion

    #region BankCards
    Task<List<BankCard>> GetBankCardsByOfficeIdAsync(Guid organizationId, int officeId);
    Task<List<BankCard>> GetBankCardsByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<BankCard?> GetBankCardByIdAsync(int bankCardId, Guid organizationId, int officeId);
    Task<BankCard> CreateAsync(BankCard bankCard, byte[] encryptedCardNumber);
    Task<BankCard> UpdateByIdAsync(BankCard bankCard, byte[] encryptedCardNumber);
    Task DeleteBankCardByIdAsync(int bankCardId, Guid organizationId, int officeId);
    #endregion

    #region ChartOfAccounts
    Task<List<ChartOfAccount>> GetChartOfAccountsByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<List<ChartOfAccount>> GetChartOfAccountsByOfficeIdAsync(Guid organizationId, int officeId);
    Task<List<ChartOfAccount>> GetChartOfAccountsByOrganizationIdAsync(Guid organizationId);
    Task<ChartOfAccount?> GetChartOfAccountByIdAsync(Guid organizationId, int officeId, int accountId);
    Task<ChartOfAccount?> GetChartOfAccountByAccountNoAsync(Guid organizationId, int officeId, string accountNo);
    Task<bool> ExistsChartOfAccountByAccountIdAsync(Guid organizationId, int officeId, int accountId);
    Task<bool> ExistsChartOfAccountByAccountNoAsync(Guid organizationId, int officeId, string accountNo, int? excludeAccountId = null);

    Task<ChartOfAccount> CreateAsync(ChartOfAccount chartOfAccount);
    Task<ChartOfAccount> UpdateChartOfAccountByIdAsync(ChartOfAccount chartOfAccount);
    Task<ChartOfAccount> UpdateChartOfAccountReconcileByIdAsync(Guid organizationId, int officeId, int accountId, decimal endingBalance, DateOnly statementDate);
    Task DeleteChartOfAccountByIdAsync(Guid organizationId, int officeId, int accountId);
    #endregion

    #region Reconcile
    Task<List<Reconcile>> GetReconcilesByAccountIdAsync(Guid organizationId, int officeId, int accountId);
    Task<Reconcile?> GetReconcileByIdAsync(int reconcileId, Guid organizationId, int officeId);
    Task<Reconcile> CreateReconcileAsync(Reconcile reconcile);
    Task<Reconcile> UpdateReconcileByIdAsync(Reconcile reconcile);
    Task DeleteReconcileByIdAsync(int reconcileId, Guid organizationId, int officeId);

    Task<ReconcileDraft?> GetReconcileDraftByAccountIdAsync(Guid organizationId, int officeId, int accountId);
    Task<ReconcileDraft> UpsertReconcileDraftAsync(ReconcileDraft reconcileDraft);
    Task DeleteReconcileDraftByAccountIdAsync(Guid organizationId, int officeId, int accountId);
    #endregion

    #region AccountingErrors
    Task LogAccountingErrorAsync(AccountingError error);
    #endregion

    #region AccountingLogs
    Task LogAccountingLogAsync(AccountingLog log);
    Task DeleteAllAccountingLogsByOrganizationIdAsync(Guid organizationId);
    #endregion

    #region Get
    Task<IEnumerable<Deposit>> GetDepositsByCriteriaAsync(DepositGetCriteria criteria);
    Task<IEnumerable<Deposit>> GetDepositsByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<Deposit>> GetDepositsByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess);
    Task<Deposit?> GetDepositByIdAsync(Guid depositId, Guid organizationId);
    #endregion

    #region Post
    Task<Deposit> CreateDepositAsync(Deposit deposit);
    #endregion

    #region Put
    Task<Deposit> UpdateDepositAsync(Deposit deposit);
    #endregion

    #region Delete
    Task DeleteDepositByIdAsync(Guid depositId, Guid organizationId, Guid currentUser);
    #endregion

    #region Get
    Task<IEnumerable<Payment>> GetPaymentsByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<Payment?> GetPaymentByIdAsync(Guid paymentId, Guid organizationId);
    #endregion

    #region Post
    Task<Payment> CreatePaymentAsync(Payment payment);
    Task SetLedgerLinePaymentIdAsync(Guid ledgerLineId, Guid paymentId, Guid modifiedBy);
    #endregion

    #region Put
    Task<Payment> UpdatePaymentAsync(Payment payment);
    #endregion

    #region Delete
    Task DeletePaymentByIdAsync(Guid paymentId, Guid organizationId, Guid currentUser);
    #endregion

    #region Get
    Task<IEnumerable<Transfer>> GetTransfersByCriteriaAsync(TransferGetCriteria criteria);
    Task<IEnumerable<Transfer>> GetTransfersByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<Transfer>> GetTransfersByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess);
    Task<Transfer?> GetTransferByIdAsync(Guid transferId, Guid organizationId);
    #endregion

    #region Post
    Task<Transfer> CreateTransferAsync(Transfer transfer);
    #endregion

    #region Put
    Task<Transfer> UpdateTransferAsync(Transfer transfer);
    #endregion

    #region Delete
    Task DeleteTransferByIdAsync(Guid transferId, Guid organizationId, Guid currentUser);
    #endregion

    #region CheckHtml
    Task<CheckHtml?> GetCheckHtmlByScopeAsync(Guid organizationId, int? officeId);
    Task<CheckHtml?> GetCheckHtmlByIdAsync(Guid checkHtmlId);
    Task<List<CheckHtml>> GetCheckHtmlAllAsync(Guid? organizationId = null);
    Task<CheckHtml> CreateCheckHtmlAsync(CheckHtml checkHtml);
    Task<CheckHtml> UpdateCheckHtmlByIdAsync(CheckHtml checkHtml);
    Task DeleteCheckHtmlByIdAsync(Guid checkHtmlId);
    #endregion
}

using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Managers;

public interface IAccountingManager
{
    #region Ledger Lines
    Task<List<LedgerLine>> CreateLedgerLinesForOrganizationIdAsync(Organization organization, DateOnly startDate, DateOnly endDate);
    Task ApplyBillingCostCodesAsync(Guid organizationId, List<LedgerLine> ledgerLines);
    Task CreateDefaultCostCodeAsync(Guid organizationId, int officeId);
    Task<List<LedgerLine>> CreateLedgerLinesForReservationIdAsync(Reservation reservation, DateOnly invoiceDate, DateOnly startDate, DateOnly endDate);
    List<LedgerLine> GetLedgerLinesByReservationIdAsync(Reservation reservation, DateOnly startDate, DateOnly endDate, int rentalCostCodeId);
    #endregion

    #region Payments
    Task<InvoicePayment> ApplyPaymentToInvoicesAsync(List<Guid> invoiceGuids, Guid organizationId, string offices, int costCodeId, string description, decimal amountPaid, DateOnly paymentDate, Guid currentUser);
    Task<BillPayment> ApplyPaymentToBillsAsync(List<Guid> billIds, Guid organizationId, string offices, int chartOfAccountId, string description, decimal amountPaid, DateOnly paymentDate, PaymentType paymentType, Guid currentUser);
    #endregion

    #region Journal Entries
    Task<JournalEntry?> CreateJournalEntryAsync(JournalEntry journalEntry);
    Task<JournalEntry> UpdateJournalEntryAsync(JournalEntry journalEntry);
    Task<JournalEntry> PostJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser);
    Task<JournalEntry> UnpostJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser);
    Task<JournalEntry> VoidJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser);
    Task DeleteJournalEntryAsync(Guid journalEntryId, Guid organizationId);
    Task<JournalEntry?> CreateJournalEntryFromReceiptAsync(Receipt receipt, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntryFromBillAsync(Receipt bill, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntryFromInvoiceAsync(Invoice invoice, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntryFromPaymentAsync(Invoice invoice, LedgerLine paymentLedgerLine, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntriesFromPrePaymentAsync(Invoice invoice, LedgerLine paymentLedgerLine, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser);
    Task<JournalEntry?> CreatePrePaymentReceivedJournalEntryAsync(Invoice invoice, LedgerLine paymentLedgerLine, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser);
    Task<JournalEntry?> CreatePrePaymentApplyJournalEntryAsync(Invoice invoice, LedgerLine paymentLedgerLine, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser);
    Task<List<JournalEntry>> CreateJournalEntriesFromInvoicePaymentAsync(InvoicePayment invoicePayment, Guid currentUser);
    Task<List<JournalEntry>> CreateJournalEntriesFromBillPaymentAsync(BillPayment billPayment, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntryFromDepositAsync(int officeId, Guid organizationId, int bankChartOfAccountId, string description, decimal amount, DateOnly depositDate, List<Guid> journalEntryLineIds, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntryFromWorkOrderAsync(WorkOrder workOrder, Guid currentUser);
    Task DeleteJournalEntriesForInvoiceAsync(Invoice invoice);
    Task DeleteJournalEntriesForReceiptAsync(Receipt receipt);
    #endregion

    #region Document Updates
    Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
    Task<Receipt> UpdateBillAsync(Receipt bill, Guid currentUser);
    Task<Receipt> UpdateReceiptAsync(Receipt receipt, Guid currentUser);
    Task<WorkOrder> UpdateWorkOrderAsync(WorkOrder workOrder, Guid currentUser);
    #endregion

    #region Owner Starting Balance
    Task<OwnerStatementStartingBalanceEntry?> GetOwnerStatementStartingBalanceAsync(Guid organizationId, int officeId, Guid ownerId, Guid propertyId);
    Task<JournalEntry?> CreateOwnerStatementStartingBalanceJournalEntryAsync(Guid organizationId, int officeId, Guid ownerId, Guid propertyId, DateOnly transactionDate, decimal amount, Guid currentUser);
    #endregion

    #region Journal Entry Sync
    Task<JournalEntrySyncResult> SyncInvoiceJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> ClearInvoiceJournalEntriesAsync(Guid organizationId, string officeIds);
    Task<JournalEntrySyncResult> SyncBillJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> ClearBillJournalEntriesAsync(Guid organizationId, string officeIds);
    Task<JournalEntrySyncResult> SyncReceiptJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> ClearReceiptJournalEntriesAsync(Guid organizationId, string officeIds);
    Task<JournalEntrySyncResult> SyncWorkOrderJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> SyncPeriodicFeeJournalEntriesAsync(
        Guid organizationId,
        string officeIds,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> ClearAllJournalEntriesAsync(Guid organizationId, string officeIds);
    #endregion

    #region Periodic Tasks
    Task CreateJournalEntiesForDepartedReservationAsync(Guid organizationId, IReadOnlyCollection<ReservationList> reservations, CancellationToken cancellationToken);
    Task CreateJournalEntriesForLinensAndTowelsAsync(Guid organizationId, IReadOnlyCollection<PropertyAgreement> monthlyAgreements, IReadOnlyCollection<PropertyAgreement> annualAgreements, CancellationToken cancellationToken, DateOnly? processingDate = null);
    #endregion
}

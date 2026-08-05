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
    Task<IReadOnlyList<Invoice>> GetPreBillingInvoicesAsync(Guid organizationId, string officeIds, DateOnly billingMonth);
    Task<IReadOnlyList<Invoice>> GetMissingInvoicesAsync(Guid organizationId, string officeIds);
    Task<IReadOnlyList<Invoice>> GetReservationInvoicePreviewsAsync(Guid organizationId, Guid reservationId);
    #endregion

    #region Payments
    Task<InvoicePayment> ApplyPaymentToInvoicesAsync(List<Guid> invoiceGuids, Guid organizationId, string offices, int costCodeId, string description, decimal amountPaid, DateOnly paymentDate, Guid currentUser);
    Task<Payment> ApplyInvoicePaymentAsync(Payment payment, IReadOnlyList<Guid>? autoSplitInvoiceIds, IReadOnlyList<PaymentInvoiceAllocation>? explicitAllocations, string officeAccess, Guid currentUser);
    Task<BillPayment> ApplyPaymentToBillsAsync(List<Guid> billIds, Guid organizationId, string offices, int chartOfAccountId, string description, decimal amountPaid, DateOnly paymentDate, PaymentType paymentType, Guid currentUser);
    Task<List<JournalEntry>> ApplyPaymentToOwnersAsync(OwnerPayments ownerPayments, Guid organizationId, string offices, Guid currentUser);
    Task<List<JournalEntry>> CreateJournalEntriesFromOwnerPaymentAsync(OwnerPaymentBatch ownerPaymentBatch, Guid currentUser);
    Task<Reservation> ApplySecurityDepositReturnAsync(Guid reservationId, Guid organizationId, string officeAccess, int chartOfAccountId, string description, decimal amount, DateOnly paymentDate, PaymentType paymentType, Guid currentUser);
    Task<Reservation> ApplySecurityDepositTransferAsync(Guid reservationId, Guid organizationId, string officeAccess, int chartOfAccountId, string description, decimal amount, DateOnly paymentDate, PaymentType paymentType, Guid currentUser);
    Task<UnreturnedSecurityDepositsResult> GetUnreturnedSecurityDepositsAsync(Guid organizationId, string officeAccess, int? officeId = null);
    Task<SecurityDepositDetailResult> GetSecurityDepositDetailAsync(Guid reservationId, Guid organizationId, string officeAccess);
    #endregion

    #region Journal Entries
    Task<JournalEntry?> CreateJournalEntryAsync(JournalEntry journalEntry);
    Task<JournalEntry> UpdateJournalEntryAsync(JournalEntry journalEntry);
    Task<JournalEntry> PostJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser, DateOnly? accountingPeriod = null);
    Task<JournalEntry> UnpostJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser);
    Task<JournalEntry> VoidJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser);
    Task<JournalEntry> SoftCloseJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser);
    Task<JournalEntry> HardCloseJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser);
    Task<CloseAccountingPeriodResult> CloseAccountingPeriodAsync(Guid organizationId, int officeId, DateOnly startDate, DateOnly endDate, PostingStatus closeStatus, IEnumerable<Guid> journalEntryIds, Guid currentUser);
    Task DeleteJournalEntryAsync(Guid journalEntryId, Guid organizationId);
    Task<JournalEntry?> CreateJournalEntryFromReceiptAsync(Receipt receipt, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntryFromBillAsync(Receipt bill, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntryFromInvoiceAsync(Invoice invoice, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntryFromPaymentAsync(Invoice invoice, LedgerLine paymentLedgerLine, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntriesFromPrePaymentAsync(Invoice invoice, LedgerLine paymentLedgerLine, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser);
    Task<JournalEntry?> CreatePrePaymentReceivedJournalEntryAsync(Invoice invoice, LedgerLine paymentLedgerLine, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser);
    Task<JournalEntry?> CreatePrePaymentApplyJournalEntryAsync(Invoice invoice, LedgerLine paymentLedgerLine, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser);
    Task<List<JournalEntry>> CreateJournalEntriesFromPaymentDocumentAsync(Guid paymentId, Guid organizationId, Guid currentUser);
    Task<List<JournalEntry>> CreateJournalEntriesFromInvoicePaymentAsync(InvoicePayment invoicePayment, Guid currentUser);
    Task<List<JournalEntry>> CreateJournalEntriesFromBillPaymentAsync(BillPayment billPayment, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntryFromDepositAsync(Deposit deposit, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntryFromTransferAsync(Transfer transfer, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntryFromWorkOrderAsync(WorkOrder workOrder, Guid currentUser);
    Task<int> DeactivateInvoicesByReservationIdAsync(Guid organizationId, Guid reservationId, Guid modifiedBy);
    Task<int> ReactivateInvoicesByReservationIdAsync(Guid organizationId, Guid reservationId, Guid modifiedBy);
    #endregion

    #region Document Lifecycle

    #region Invoice
    Task<Invoice> CreateInvoiceAsync(Invoice invoice, Guid currentUser);
    Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
    Task DeleteInvoiceAsync(Guid invoiceId, Guid organizationId);
    Task DeleteInvoicesByReservationIdAsync(Guid organizationId, Guid reservationId);
    #endregion

    #region Receipt
    Task<Receipt> CreateReceiptAsync(Receipt receipt, Guid currentUser);
    Task<Receipt> UpdateBillAsync(Receipt bill, Guid currentUser);
    Task<Receipt> UpdateReceiptAsync(Receipt receipt, Guid currentUser);
    Task DeleteReceiptAsync(Guid receiptId, Guid organizationId, Guid currentUser);
    #endregion

    #region Work Order
    Task<WorkOrder> CreateWorkOrderAsync(WorkOrder workOrder, Guid currentUser);
    Task<WorkOrder> UpdateWorkOrderAsync(WorkOrder workOrder, Guid currentUser);
    Task DeleteWorkOrderAsync(Guid workOrderId, Guid organizationId, Guid currentUser);
    #endregion

    #region Payment
    Task<Payment> CreatePaymentAsync(Payment payment, Guid currentUser);
    Task<Payment> CreatePaymentWithInvoiceAllocationsAsync(Payment payment, IReadOnlyList<PaymentInvoiceAllocation> allocations, string officeAccess, Guid currentUser);
    Task<Payment> UpdatePaymentAsync(Payment payment, Guid currentUser);
    Task<Payment> UpdatePaymentWithInvoiceAllocationsAsync(Payment payment, IReadOnlyList<PaymentInvoiceAllocation> allocations, string officeAccess, Guid currentUser);
    Task DeletePaymentAsync(Guid paymentId, Guid organizationId, Guid currentUser);
    #endregion

    #region Deposit
    Task<Deposit> CreateDepositAsync(Deposit deposit, Guid currentUser);
    Task<Deposit> UpdateDepositAsync(Deposit deposit, Guid currentUser);
    Task DeleteDepositAsync(Guid depositId, Guid organizationId, Guid currentUser);
    #endregion

    #region Transfer
    Task<Transfer> CreateTransferAsync(Transfer transfer, Guid currentUser);
    Task<Transfer> UpdateTransferAsync(Transfer transfer, Guid currentUser);
    Task<Transfer> PostTransferReportAsync(Guid transferId, Guid organizationId, Guid currentUser);
    Task DeleteTransferAsync(Guid transferId, Guid organizationId, Guid currentUser);
    Task EnrichTransferSplitsForDisplayAsync(Transfer transfer);
    Task<IReadOnlyList<TransferDepositAllocationResult>> ResolveTransferDepositAllocationsAsync(Guid organizationId, int officeId, IReadOnlyList<TransferDepositAllocationRequestItem> items);
    Task<IReadOnlyList<TransferReportLineAllocationResult>> ResolveTransferReportLineAllocationsAsync(Guid organizationId, Guid transferId);
    #endregion

    Task ApplyDocumentPostingStatusFromReconcileAsync(CompleteReconcileRequest request, Guid organizationId, Guid currentUser);
    #endregion

    #region Default Chart Of Accounts
    int GetDefaultOwnerAccountsPayable(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice);
    int GetDefaultEscrowOwnersAccount(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice);
    int GetDefaultRetainedEarningsAccount(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice);
    #endregion

    #region Owner AP Aging
    Task<IReadOnlyList<JournalEntryLineSearchResult>> SearchOwnerApAgingJournalEntryLinesAsync(Guid organizationId, IReadOnlyList<int> officeIds, DateOnly? endDate, bool includeUnposted = true);
    Task<IReadOnlyList<JournalEntryLineSearchResult>> FilterOwnerApAgingJournalEntryLinesAsync(Guid organizationId, IReadOnlyList<JournalEntryLineSearchResult> lines);
    #endregion

    #region Journal Entry Sync
    Task<JournalEntrySyncResult> SyncInvoiceJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> SyncPaymentJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> ClearInvoiceJournalEntriesAsync(Guid organizationId, string officeIds);
    Task<JournalEntrySyncResult> SyncBillJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> ClearBillJournalEntriesAsync(Guid organizationId, string officeIds);
    Task<JournalEntrySyncResult> SyncReceiptJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> ClearReceiptJournalEntriesAsync(Guid organizationId, string officeIds);
    Task<JournalEntrySyncResult> SyncWorkOrderJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> SyncDepositJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> SyncTransferJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null);
    Task SyncDocumentLinksAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> SyncPeriodicFeeJournalEntriesAsync(
        Guid organizationId,
        string officeIds,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        IProgress<JournalEntrySyncProgress>? progress = null);
    Task<JournalEntrySyncResult> ClearAllJournalEntriesAsync(Guid organizationId, string officeIds);
    #endregion

    #region Periodic Tasks
    Task<int> ProcessDepartureFeesAsync(Guid organizationId, string officeIds, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default, bool logDecisions = false);
    Task CreateJournalEntriesForDepartedReservationsAsync(Guid organizationId, IReadOnlyCollection<ReservationDeparture> reservations, CancellationToken cancellationToken, bool logDecisions = false);
    Task CreateJournalEntriesForLinensAndTowelsAsync(Guid organizationId, IReadOnlyCollection<PropertyAgreement> monthlyAgreements, IReadOnlyCollection<PropertyAgreement> annualAgreements, CancellationToken cancellationToken, DateOnly? processingDate = null);
    Task<int> ProcessRetainedEarningsAsync(Guid organizationId, string officeIds, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default, bool logDecisions = false);
    Task CreateJournalEntriesForRetainedEarningsAsync(Guid organizationId, IReadOnlyCollection<AccountingOffice> accountingOffices, DateOnly processingDate, CancellationToken cancellationToken, bool logDecisions = false);
    Task<JournalEntry> PreviewRetainedEarningsJournalEntryForFiscalYearEndAsync(Guid organizationId, int officeId, int fiscalYearEndYear, CancellationToken cancellationToken = default);
    #endregion
}

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
    Task<JournalEntry?> GetOrCreatePrePaymentReceivedJournalEntryAsync(Invoice invoice, LedgerLine paymentLedgerLine, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser);
    Task<JournalEntry?> GetOrCreatePrePaymentApplyJournalEntryAsync(Invoice invoice, LedgerLine paymentLedgerLine, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice, Guid currentUser);
    Task<List<JournalEntry>> CreateJournalEntriesFromInvoicePaymentAsync(InvoicePayment invoicePayment, Guid currentUser);
    Task<List<JournalEntry>> CreateJournalEntriesFromBillPaymentAsync(BillPayment billPayment, Guid currentUser);
    Task<JournalEntry?> CreateJournalEntryFromDepositAsync(int officeId, Guid organizationId, int bankChartOfAccountId, string description, decimal amount, DateOnly depositDate, List<Guid> journalEntryLineIds, Guid currentUser);
    #endregion

    #region Document Updates
    Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
    Task<Receipt> UpdateBillAsync(Receipt bill, Guid currentUser);
    Task<Receipt> UpdateReceiptAsync(Receipt receipt, Guid currentUser);
    #endregion

    #region Journal Entry Sync
    Task<JournalEntrySyncResult> SyncInvoiceJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser);
    Task<JournalEntrySyncResult> ClearInvoiceJournalEntriesAsync(Guid organizationId, string officeIds);
    Task<JournalEntrySyncResult> SyncBillJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser);
    Task<JournalEntrySyncResult> ClearBillJournalEntriesAsync(Guid organizationId, string officeIds);
    Task<JournalEntrySyncResult> SyncReceiptJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser);
    Task<JournalEntrySyncResult> ClearReceiptJournalEntriesAsync(Guid organizationId, string officeIds);
    Task<JournalEntrySyncResult> ClearAllJournalEntriesAsync(Guid organizationId);
    #endregion
}

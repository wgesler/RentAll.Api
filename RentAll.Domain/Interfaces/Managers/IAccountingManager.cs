using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Managers;

public interface IAccountingManager
{
    Task<InvoicePayment> ApplyPaymentToInvoicesAsync(List<Guid> invoiceGuids, Guid organizationId, string offices, int costCodeId, string description, decimal amountPaid, DateOnly paymentDate, Guid currentUser);
    Task<BillPayment> ApplyPaymentToBillsAsync(List<int> billIds, Guid organizationId, string offices, int chartOfAccountId, string description, decimal amountPaid, DateOnly paymentDate, Guid currentUser);
    Task<List<LedgerLine>> CreateLedgerLinesForReservationIdAsync(Reservation reservation, DateOnly invoiceDate, DateOnly startDate, DateOnly endDate);
    Task<List<LedgerLine>> CreateLedgerLinesForOrganizationIdAsync(Organization organization, DateOnly startDate, DateOnly endDate);
    Task CreateDefaultCostCodeAsync(Guid organizationId, int officeId);

    Task<JournalEntry> CreateJournalEntryAsync(JournalEntry journalEntry);
    Task<JournalEntry> UpdateJournalEntryAsync(JournalEntry journalEntry);
    Task<JournalEntry> PostJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser);
    Task<JournalEntry> UnpostJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser);
    Task<JournalEntry> VoidJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser);
    Task DeleteJournalEntryAsync(Guid journalEntryId, Guid organizationId);
    Task<JournalEntry> CreateJournalEntryFromReceiptAsync(Receipt receipt, Guid currentUser);
    Task<JournalEntry> CreateJournalEntryFromBillAsync(Receipt bill, Guid currentUser);
    Task<JournalEntry> CreateJournalEntryFromInvoiceAsync(Invoice invoice, Guid currentUser);
    Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
    Task<Receipt> UpdateBillAsync(Receipt bill, Guid currentUser);
    Task<JournalEntry> CreateJournalEntryFromPaymentAsync(Invoice invoice, LedgerLine paymentLedgerLine, Guid currentUser);
    Task<List<JournalEntry>> CreateJournalEntriesFromInvoicePaymentAsync(InvoicePayment invoicePayment, Guid currentUser);
    Task<List<JournalEntry>> CreateJournalEntriesFromBillPaymentAsync(BillPayment billPayment, Guid currentUser);
    Task<JournalEntrySyncResult> SyncInvoiceJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser);
    Task<JournalEntrySyncResult> ClearInvoiceJournalEntriesAsync(Guid organizationId, string officeIds);
    Task<JournalEntrySyncResult> SyncBillJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser);
    Task<JournalEntrySyncResult> ClearBillJournalEntriesAsync(Guid organizationId, string officeIds);
    Task<JournalEntrySyncResult> SyncReceiptJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser);
    Task<JournalEntrySyncResult> ClearReceiptJournalEntriesAsync(Guid organizationId, string officeIds);
}

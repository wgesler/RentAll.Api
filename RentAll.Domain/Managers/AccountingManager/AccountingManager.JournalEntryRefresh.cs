using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
    {
        var existingInvoice = await _accountingRepository.GetInvoiceByIdAsync(invoice.InvoiceId, invoice.OrganizationId);
        if (existingInvoice == null)
            throw new Exception("Invoice not found");

        var priorPaymentLedgerLineIds = existingInvoice.LedgerLines
            .Where(l => l.LedgerLineId != Guid.Empty)
            .Select(l => l.LedgerLineId);

        var updatedInvoice = await _accountingRepository.UpdateByIdAsync(invoice);

        try
        {
            await ReplaceJournalEntriesFromInvoiceAsync(updatedInvoice, priorPaymentLedgerLineIds);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invoice was updated but general ledger entry refresh failed: {ex.Message}", ex);
        }

        return updatedInvoice;
    }

    public async Task<Receipt> UpdateBillAsync(Receipt bill, Guid currentUser)
    {
        EnsureReceiptIsBill(bill);

        var updatedBill = await _maintenanceRepository.UpdateReceiptAsync(bill);

        try
        {
            await ReplaceJournalEntriesFromBillAsync(updatedBill, currentUser);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Bill was updated but general ledger entry refresh failed: {ex.Message}", ex);
        }

        return updatedBill;
    }

    async Task ReplaceJournalEntriesFromInvoiceAsync(Invoice invoice, IEnumerable<Guid> priorPaymentLedgerLineIds)
    {
        if (invoice.InvoiceId == Guid.Empty)
            throw new Exception("InvoiceId is required to refresh journal entries");

        var costCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var costCodeById = costCodes.ToDictionary(c => c.CostCodeId);

        var paymentLedgerLineIds = priorPaymentLedgerLineIds
            .Concat(invoice.LedgerLines
                .Where(l => l.LedgerLineId != Guid.Empty)
                .Where(l => costCodeById.TryGetValue(l.CostCodeId, out var costCode) && IsPaymentLedgerLine(costCode))
                .Select(l => l.LedgerLineId))
            .Distinct()
            .ToList();

        await DeleteJournalEntriesForSourceAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            (int)SourceType.Invoice,
            invoice.InvoiceId);

        foreach (var ledgerLineId in paymentLedgerLineIds)
        {
            await DeleteJournalEntriesForSourceAsync(
                invoice.OrganizationId,
                invoice.OfficeId,
                (int)SourceType.InvoicePayment,
                ledgerLineId);
        }

        if (IsAccountingFeatureEnabled())
        {
            await CreateJournalEntryFromInvoiceAsync(invoice, invoice.ModifiedBy);

            foreach (var paymentLedgerLine in invoice.LedgerLines
                         .Where(l => l.Amount != 0)
                         .Where(l => costCodeById.TryGetValue(l.CostCodeId, out var costCode) && IsPaymentLedgerLine(costCode)))
            {
                await CreateJournalEntryFromPaymentAsync(invoice, paymentLedgerLine, invoice.ModifiedBy);
            }
        }
    }

    async Task ReplaceJournalEntriesFromBillAsync(Receipt bill, Guid currentUser)
    {
        EnsureReceiptIsBill(bill);

        if (bill.ReceiptGuid == Guid.Empty)
            throw new Exception("ReceiptGuid is required to refresh journal entries");

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(bill.OrganizationId, bill.OfficeId);
        var paymentOffsetAccountId = await DeleteBillPaymentJournalEntriesAsync(bill, chartOfAccounts, accountingOffice);

        await DeleteJournalEntriesForSourceAsync(
            bill.OrganizationId,
            bill.OfficeId,
            (int)SourceType.Bill,
            bill.ReceiptGuid);

        if (IsAccountingFeatureEnabled())
        {
            var billJournalEntry = await BuildJournalEntryFromBillAsync(bill, chartOfAccounts, accountingOffice, currentUser);
            await CreateJournalEntryAsync(billJournalEntry);
        }

        if (bill.PaidAmount == 0)
            return;

        var billLabel = !string.IsNullOrWhiteSpace(bill.BillNumber)
            ? bill.BillNumber.Trim()
            : bill.ReceiptCode.Trim();
        var paymentApplication = new BillPaymentApplication
        {
            Bill = bill,
            AmountApplied = bill.PaidAmount,
            PaymentDate = bill.PaidDate ?? bill.ReceiptDate,
            ChartOfAccountId = paymentOffsetAccountId ?? GetBankAccountId(chartOfAccounts, bill.OfficeId, accountingOffice),
            Description = $"Bill Payment - {billLabel}",
            PaymentSequence = 0
        };

        var paymentJournalEntry = await BuildJournalEntryFromBillPaymentAsync(paymentApplication, chartOfAccounts, accountingOffice, currentUser);
        await CreateJournalEntryAsync(paymentJournalEntry);
    }

    async Task DeleteJournalEntriesForSourceAsync(Guid organizationId, int officeId, int sourceTypeId, Guid sourceId)
    {
        var entries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeId.ToString(),
            SourceTypeId = sourceTypeId,
            SourceId = sourceId,
            IncludeVoided = true,
            IncludeUnposted = true
        })).ToList();

        foreach (var entry in entries.Where(e => !e.IsVoided))
        {
            if (entry.IsPosted)
                throw new Exception($"Cannot refresh journal entries because {entry.JournalEntryCode} is posted");

            await DeleteJournalEntryAsync(entry.JournalEntryId, organizationId);
        }
    }

    async Task<int?> DeleteBillPaymentJournalEntriesAsync(Receipt bill, List<ChartOfAccount> chartOfAccounts, AccountingOffice? accountingOffice)
    {
        var liabilityAccountId = await GetBillLiabilityAccountIdAsync(bill, chartOfAccounts, accountingOffice);

        var existingEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = bill.OrganizationId,
            OfficeIds = bill.OfficeId.ToString(),
            SourceTypeId = (int)SourceType.BillPayment,
            IncludeVoided = true,
            IncludeUnposted = true
        })).ToList();

        int? paymentOffsetAccountId = null;

        foreach (var entry in existingEntries.Where(e => !e.IsVoided && e.SourceId is Guid sourceId && TryGetBillPaymentSequence(sourceId, bill.ReceiptGuid) >= 0))
        {
            paymentOffsetAccountId ??= ResolveBillPaymentOffsetAccountId(entry, liabilityAccountId);

            if (entry.IsPosted)
                throw new Exception($"Cannot refresh journal entries because {entry.JournalEntryCode} is posted");

            await DeleteJournalEntryAsync(entry.JournalEntryId, bill.OrganizationId);
        }

        return paymentOffsetAccountId;
    }

    static int? ResolveBillPaymentOffsetAccountId(JournalEntry entry, int liabilityAccountId)
    {
        var offsetLine = entry.JournalEntryLines.FirstOrDefault(l =>
            l.ChartOfAccountId > 0 &&
            l.ChartOfAccountId != liabilityAccountId);

        return offsetLine?.ChartOfAccountId;
    }
}

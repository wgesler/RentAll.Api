using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<JournalEntrySyncResult> SyncInvoiceJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser)
    {
        var result = new JournalEntrySyncResult();
        var invoices = (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true,
            IncludePaid = true
        })).ToList();

        var costCodesByOffice = new Dictionary<int, Dictionary<int, CostCode>>();

        foreach (var invoiceSummary in invoices)
        {
            result.DocumentsProcessed++;

            try
            {
                var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceSummary.InvoiceId, organizationId);
                if (invoice == null)
                    continue;

                await TrackJournalEntryCreateAsync(
                    () => CreateJournalEntryFromInvoiceAsync(invoice, currentUser),
                    new JournalEntryGetCriteria
                    {
                        OrganizationId = invoice.OrganizationId,
                        OfficeIds = invoice.OfficeId.ToString(),
                        SourceTypeId = (int)SourceType.Invoice,
                        SourceId = invoice.InvoiceId,
                        IncludeVoided = true,
                        IncludeUnposted = true
                    },
                    result);

                if (!costCodesByOffice.TryGetValue(invoice.OfficeId, out var costCodeById))
                {
                    var costCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(organizationId, invoice.OfficeId);
                    costCodeById = costCodes.ToDictionary(c => c.CostCodeId);
                    costCodesByOffice[invoice.OfficeId] = costCodeById;
                }

                foreach (var line in invoice.LedgerLines.Where(l => l.Amount != 0))
                {
                    costCodeById.TryGetValue(line.CostCodeId, out var costCode);
                    if (!IsPaymentLedgerLine(costCode))
                        continue;

                    await TrackJournalEntryCreateAsync(
                        () => CreateJournalEntryFromPaymentAsync(invoice, line, currentUser),
                        new JournalEntryGetCriteria
                        {
                            OrganizationId = invoice.OrganizationId,
                            OfficeIds = invoice.OfficeId.ToString(),
                            SourceTypeId = (int)SourceType.InvoicePayment,
                            SourceId = line.LedgerLineId,
                            IncludeVoided = true,
                            IncludeUnposted = true
                        },
                        result);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Invoice {invoiceSummary.InvoiceCode}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<JournalEntrySyncResult> ClearInvoiceJournalEntriesAsync(Guid organizationId, string officeIds)
    {
        return await ClearJournalEntriesBySourceTypesAsync(
            organizationId,
            officeIds,
            deletePostedEntries: true,
            (int)SourceType.Invoice,
            (int)SourceType.InvoicePayment);
    }

    public async Task<JournalEntrySyncResult> SyncBillJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser)
    {
        var result = new JournalEntrySyncResult();
        var bills = (await _maintenanceRepository.GetReceiptsByCriteriaAsync(new ReceiptGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true,
            ReceiptKind = ReceiptKind.Bill
        })).ToList();

        foreach (var billSummary in bills)
        {
            result.DocumentsProcessed++;

            try
            {
                var bill = await _maintenanceRepository.GetReceiptByIdAsync(billSummary.ReceiptId, organizationId);
                if (bill == null)
                    continue;

                EnsureReceiptIsBill(bill);

                await TrackJournalEntryCreateAsync(
                    () => CreateJournalEntryFromBillAsync(bill, currentUser),
                    new JournalEntryGetCriteria
                    {
                        OrganizationId = bill.OrganizationId,
                        OfficeIds = bill.OfficeId.ToString(),
                        SourceTypeId = (int)SourceType.Bill,
                        SourceId = bill.ReceiptId,
                        IncludeVoided = true,
                        IncludeUnposted = true
                    },
                    result);

                if (bill.PaidAmount != 0)
                {
                    try
                    {
                        await SyncBillPaymentJournalEntryAsync(bill, currentUser, result);
                    }
                    catch (Exception paymentEx)
                    {
                        var paymentBillLabel = !string.IsNullOrWhiteSpace(billSummary.BillNumber)
                            ? billSummary.BillNumber
                            : billSummary.ReceiptCode;
                        result.Errors.Add($"Bill {paymentBillLabel} payment: {paymentEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                var billLabel = !string.IsNullOrWhiteSpace(billSummary.BillNumber)
                    ? billSummary.BillNumber
                    : billSummary.ReceiptCode;
                result.Errors.Add($"Bill {billLabel}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<JournalEntrySyncResult> ClearBillJournalEntriesAsync(Guid organizationId, string officeIds)
    {
        return await ClearJournalEntriesBySourceTypesAsync(
            organizationId,
            officeIds,
            deletePostedEntries: false,
            (int)SourceType.Bill,
            (int)SourceType.BillPayment);
    }

    public async Task<JournalEntrySyncResult> SyncReceiptJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser)
    {
        var result = new JournalEntrySyncResult();
        var receipts = (await _maintenanceRepository.GetReceiptsByCriteriaAsync(new ReceiptGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true,
            ReceiptKind = ReceiptKind.Card
        })).ToList();

        foreach (var receiptSummary in receipts)
        {
            result.DocumentsProcessed++;

            try
            {
                var receipt = await _maintenanceRepository.GetReceiptByIdAsync(receiptSummary.ReceiptId, organizationId);
                if (receipt == null)
                    continue;

                EnsureReceiptIsCardReceipt(receipt);

                await TrackJournalEntryCreateAsync(
                    () => CreateJournalEntryFromReceiptAsync(receipt, currentUser),
                    new JournalEntryGetCriteria
                    {
                        OrganizationId = receipt.OrganizationId,
                        OfficeIds = receipt.OfficeId.ToString(),
                        SourceTypeId = (int)SourceType.Receipt,
                        SourceId = receipt.ReceiptId,
                        IncludeVoided = true,
                        IncludeUnposted = true
                    },
                    result);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Receipt {receiptSummary.ReceiptCode}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<JournalEntrySyncResult> ClearReceiptJournalEntriesAsync(Guid organizationId, string officeIds)
    {
        return await ClearJournalEntriesBySourceTypesAsync(
            organizationId,
            officeIds,
            deletePostedEntries: false,
            (int)SourceType.Receipt);
    }

    public async Task<JournalEntrySyncResult> ClearAllJournalEntriesAsync(Guid organizationId)
    {
        var result = new JournalEntrySyncResult();

        try
        {
            result.JournalEntriesDeleted = await _journalEntryRepository.DeleteAllJournalEntriesByOrganizationIdAsync(organizationId);
            await _organizationManager.ResetEntityCodeSequenceAsync(organizationId, EntityType.JournalEntry, 0);
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    private async Task SyncBillPaymentJournalEntryAsync(Receipt bill, Guid currentUser, JournalEntrySyncResult result)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(bill.OrganizationId, bill.OfficeId);
        var billLabel = !string.IsNullOrWhiteSpace(bill.BillNumber)
            ? bill.BillNumber.Trim()
            : bill.ReceiptCode.Trim();
        var paymentApplication = new BillPaymentApplication
        {
            Bill = bill,
            AmountApplied = bill.PaidAmount,
            PaymentDate = bill.PaidDate ?? bill.ReceiptDate,
            ChartOfAccountId = GetDefaultBankAccount(chartOfAccounts, bill.OfficeId, accountingOffice),
            Description = $"Bill Payment - {billLabel}",
            PaymentSequence = 0
        };

        await TrackJournalEntryCreateAsync(
            () => CreateJournalEntryFromBillPaymentAsync(paymentApplication, currentUser),
            new JournalEntryGetCriteria
            {
                OrganizationId = bill.OrganizationId,
                OfficeIds = bill.OfficeId.ToString(),
                SourceTypeId = (int)SourceType.BillPayment,
                SourceId = bill.ReceiptId,
                IncludeVoided = true,
                IncludeUnposted = true
            },
            result);
    }

    private async Task<JournalEntrySyncResult> ClearJournalEntriesBySourceTypesAsync(Guid organizationId, string officeIds, bool deletePostedEntries, params int[] sourceTypeIds)
    {
        var result = new JournalEntrySyncResult();

        foreach (var sourceTypeId in sourceTypeIds)
        {
            var entries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeIds,
                SourceTypeId = sourceTypeId,
                IncludeVoided = true,
                IncludeUnposted = true
            })).ToList();

            foreach (var entry in entries)
            {
                try
                {
                    if (entry.IsPosted && !deletePostedEntries)
                    {
                        result.Errors.Add($"Cannot delete posted journal entry {entry.JournalEntryCode}");
                        continue;
                    }

                    if (deletePostedEntries && entry.IsPosted)
                        await DeleteJournalEntryIgnoringPostedStatusAsync(entry.JournalEntryId, organizationId);
                    else
                        await DeleteJournalEntryAsync(entry.JournalEntryId, organizationId);

                    result.JournalEntriesDeleted++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Journal entry {entry.JournalEntryCode}: {ex.Message}");
                }
            }
        }

        await _organizationManager.ResetEntityCodeSequenceAsync(organizationId, EntityType.JournalEntry, 0);

        return result;
    }

    private async Task TrackJournalEntryCreateAsync(Func<Task<JournalEntry?>> createJournalEntry, JournalEntryGetCriteria existingCriteria, JournalEntrySyncResult result)
    {
        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(existingCriteria);
        var hadExisting = existingEntries.Any(e => !e.IsVoided);

        var created = await createJournalEntry();
        if (created == null)
            return;

        if (hadExisting)
            result.JournalEntriesSkipped++;
        else
            result.JournalEntriesCreated++;
    }

    private static int ResolveDefaultPaymentCostCodeId(List<CostCode> costCodes)
    {
        var paymentCostCode = costCodes
            .Where(c => c.TransactionType == TransactionType.Payment)
            .OrderBy(c => c.CostCodeId)
            .FirstOrDefault();

        if (paymentCostCode == null)
            throw new Exception("No payment cost code is configured");

        return paymentCostCode.CostCodeId;
    }
}

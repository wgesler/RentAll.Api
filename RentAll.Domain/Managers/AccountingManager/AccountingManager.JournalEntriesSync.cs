using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<JournalEntrySyncResult> SyncInvoiceJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
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
        var total = invoices.Count;
        var processed = 0;
        ReportSyncProgress(progress, "invoice", total, processed, result, "Running");

        foreach (var invoiceSummary in invoices)
        {
            result.DocumentsProcessed++;

            try
            {
                var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceSummary.InvoiceId, organizationId);
                if (invoice == null)
                    continue;

                await TrackJournalEntryCreateAsync(
                    () => CreateJournalEntryFromInvoiceWithResultAsync(invoice, currentUser),
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
                        () => CreateJournalEntryFromPaymentWithResultAsync(invoice, line, currentUser),
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
                var message = $"Invoice {invoiceSummary.InvoiceCode}: {ex.Message}";
                result.Errors.Add(message);
                await LogAccountingErrorAsync(
                    trigger: "Invoice",
                    organizationId: organizationId,
                    officeId: invoiceSummary.OfficeId,
                    sourceTypeId: (int)SourceType.Invoice,
                    sourceId: invoiceSummary.InvoiceId,
                    documentCode: invoiceSummary.InvoiceCode,
                    accountingPeriod: null,
                    amount: invoiceSummary.TotalAmount,
                    message: message,
                    currentUser: currentUser);
            }

            processed++;
            ReportSyncProgress(progress, "invoice", total, processed, result, processed >= total ? "Completed" : "Running");
        }

        if (total == 0)
            ReportSyncProgress(progress, "invoice", total, processed, result, "Completed");

        return result;
    }

    public async Task<JournalEntrySyncResult> ClearInvoiceJournalEntriesAsync(Guid organizationId, string officeIds)
    {
        return await ClearJournalEntriesBySourceTypesAsync(
            organizationId,
            officeIds,
            (int)SourceType.Invoice,
            (int)SourceType.InvoicePayment);
    }

    public async Task<JournalEntrySyncResult> SyncBillJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        var result = new JournalEntrySyncResult();
        var bills = (await _maintenanceRepository.GetReceiptsByCriteriaAsync(new ReceiptGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true,
            ReceiptKind = ReceiptKind.Bill
        })).ToList();

        var total = bills.Count;
        var processed = 0;
        ReportSyncProgress(progress, "bill", total, processed, result, "Running");

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
                    () => CreateJournalEntryFromBillWithResultAsync(bill, currentUser),
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
                        var message = $"Bill {paymentBillLabel} payment: {paymentEx.Message}";
                        result.Errors.Add(message);
                        await LogAccountingErrorAsync(
                            trigger: "BillPayment",
                            organizationId: organizationId,
                            officeId: billSummary.OfficeId,
                            sourceTypeId: (int)SourceType.BillPayment,
                            sourceId: billSummary.ReceiptId,
                            documentCode: paymentBillLabel,
                            accountingPeriod: null,
                            amount: billSummary.PaidAmount,
                            message: message,
                            currentUser: currentUser);
                    }
                }
            }
            catch (Exception ex)
            {
                var billLabel = !string.IsNullOrWhiteSpace(billSummary.BillNumber)
                    ? billSummary.BillNumber
                    : billSummary.ReceiptCode;
                var message = $"Bill {billLabel}: {ex.Message}";
                result.Errors.Add(message);
                await LogAccountingErrorAsync(
                    trigger: "Bill",
                    organizationId: organizationId,
                    officeId: billSummary.OfficeId,
                    sourceTypeId: (int)SourceType.Bill,
                    sourceId: billSummary.ReceiptId,
                    documentCode: billLabel,
                    accountingPeriod: null,
                    amount: billSummary.Amount,
                    message: message,
                    currentUser: currentUser);
            }

            processed++;
            ReportSyncProgress(progress, "bill", total, processed, result, processed >= total ? "Completed" : "Running");
        }

        if (total == 0)
            ReportSyncProgress(progress, "bill", total, processed, result, "Completed");

        return result;
    }

    public async Task<JournalEntrySyncResult> ClearBillJournalEntriesAsync(Guid organizationId, string officeIds)
    {
        return await ClearJournalEntriesBySourceTypesAsync(
            organizationId,
            officeIds,
            (int)SourceType.Bill,
            (int)SourceType.BillPayment);
    }

    public async Task<JournalEntrySyncResult> SyncReceiptJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        var result = new JournalEntrySyncResult();
        var receipts = (await _maintenanceRepository.GetReceiptsByCriteriaAsync(new ReceiptGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true,
            ReceiptKind = ReceiptKind.Card
        })).ToList();

        var total = receipts.Count;
        var processed = 0;
        ReportSyncProgress(progress, "receipt", total, processed, result, "Running");

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
                    () => CreateJournalEntryFromReceiptWithResultAsync(receipt, currentUser),
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
                var message = $"Receipt {receiptSummary.ReceiptCode}: {ex.Message}";
                result.Errors.Add(message);
                await LogAccountingErrorAsync(
                    trigger: "Receipt",
                    organizationId: organizationId,
                    officeId: receiptSummary.OfficeId,
                    sourceTypeId: (int)SourceType.Receipt,
                    sourceId: receiptSummary.ReceiptId,
                    documentCode: receiptSummary.ReceiptCode,
                    accountingPeriod: null,
                    amount: receiptSummary.Amount,
                    message: message,
                    currentUser: currentUser);
            }

            processed++;
            ReportSyncProgress(progress, "receipt", total, processed, result, processed >= total ? "Completed" : "Running");
        }

        if (total == 0)
            ReportSyncProgress(progress, "receipt", total, processed, result, "Completed");

        return result;
    }

    public async Task<JournalEntrySyncResult> ClearReceiptJournalEntriesAsync(Guid organizationId, string officeIds)
    {
        return await ClearJournalEntriesBySourceTypesAsync(
            organizationId,
            officeIds,
            (int)SourceType.Receipt);
    }

    public async Task<JournalEntrySyncResult> SyncWorkOrderJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        var result = new JournalEntrySyncResult();
        var workOrders = (await _maintenanceRepository.GetWorkOrdersByCriteriaAsync(new WorkOrderGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds
        })).ToList();

        var total = workOrders.Count;
        var processed = 0;
        ReportSyncProgress(progress, "workOrder", total, processed, result, "Running");

        foreach (var workOrderSummary in workOrders)
        {
            result.DocumentsProcessed++;

            try
            {
                var workOrder = await _maintenanceRepository.GetWorkOrderByIdAsync(workOrderSummary.WorkOrderId, organizationId);
                if (workOrder == null)
                    continue;

                await TrackJournalEntryCreateAsync(
                    () => CreateJournalEntryFromWorkOrderWithResultAsync(workOrder, currentUser),
                    new JournalEntryGetCriteria
                    {
                        OrganizationId = workOrder.OrganizationId,
                        OfficeIds = workOrder.OfficeId.ToString(),
                        SourceTypeId = (int)SourceType.WorkOrder,
                        SourceId = workOrder.WorkOrderId,
                        IncludeVoided = true,
                        IncludeUnposted = true
                    },
                    result);
            }
            catch (Exception ex)
            {
                var message = $"Work order {workOrderSummary.WorkOrderCode}: {ex.Message}";
                result.Errors.Add(message);
                await LogAccountingErrorAsync(
                    trigger: "WorkOrder",
                    organizationId: organizationId,
                    officeId: workOrderSummary.OfficeId,
                    sourceTypeId: (int)SourceType.WorkOrder,
                    sourceId: workOrderSummary.WorkOrderId,
                    documentCode: workOrderSummary.WorkOrderCode,
                    accountingPeriod: null,
                    amount: workOrderSummary.Amount,
                    message: message,
                    currentUser: currentUser);
            }

            processed++;
            ReportSyncProgress(progress, "workOrder", total, processed, result, processed >= total ? "Completed" : "Running");
        }

        if (total == 0)
            ReportSyncProgress(progress, "workOrder", total, processed, result, "Completed");

        return result;
    }

    public async Task<JournalEntrySyncResult> SyncPeriodicFeeJournalEntriesAsync(Guid organizationId, string officeIds, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        var result = new JournalEntrySyncResult();
        var runDates = GetMonthlyRunDatesForCurrentYear(DateOnly.FromDateTime(DateTime.UtcNow));

        await ProcessDepartureFeesAsync(organizationId, officeIds, runDates, result, progress);
        await ProcessLinenAndTowelFeesAsync(organizationId, officeIds, runDates, result, progress);

        return result;
    }

    public async Task<JournalEntrySyncResult> ClearAllJournalEntriesAsync(Guid organizationId, string officeIds)
    {
        var result = new JournalEntrySyncResult();

        try
        {
            result.JournalEntriesDeleted = await _journalEntryRepository.DeleteJournalEntriesByOfficeIdsAsync(organizationId, officeIds);
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    private async Task ProcessDepartureFeesAsync(Guid organizationId, string officeIds, IReadOnlyCollection<DateOnly> runDates, JournalEntrySyncResult result, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        var total = runDates.Count;
        var processed = 0;
        ReportSyncProgress(progress, "departureFee", total, processed, result, "Running");

        foreach (var runDate in runDates)
        {
            try
            {
                var departures = (await _reservationRepository.GetMonthlyDepartedReservationsAsync(organizationId, officeIds, runDate)).ToList();
                result.DocumentsProcessed += departures.Count;
                await CreateJournalEntiesForDepartedReservationAsync(organizationId, departures, CancellationToken.None);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Departure fees {runDate:yyyy-MM-dd}: {ex.Message}");
            }

            processed++;
            ReportSyncProgress(progress, "departureFee", total, processed, result, processed >= total ? "Completed" : "Running");
        }

        if (total == 0)
            ReportSyncProgress(progress, "departureFee", total, processed, result, "Completed");
    }

    private async Task ProcessLinenAndTowelFeesAsync(Guid organizationId, string officeIds, IReadOnlyCollection<DateOnly> runDates, JournalEntrySyncResult result, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        var total = runDates.Count;
        var processed = 0;
        ReportSyncProgress(progress, "linenAndTowelFee", total, processed, result, "Running");

        foreach (var runDate in runDates)
        {
            try
            {
                var monthlyBatch = (await _propertyRepository.GetMonthlyLinensAndTowelsAsync(organizationId, officeIds)).ToList();
                var annualBatch = (await _propertyRepository.GetAnnualLinensAndTowelsAsync(organizationId, officeIds)).ToList();
                result.DocumentsProcessed += monthlyBatch.Count + annualBatch.Count;

                await CreateJournalEntriesForLinensAndTowelsAsync(
                    organizationId,
                    monthlyBatch,
                    annualBatch,
                    CancellationToken.None,
                    runDate);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Linen and towel fees {runDate:yyyy-MM-dd}: {ex.Message}");
            }

            processed++;
            ReportSyncProgress(progress, "linenAndTowelFee", total, processed, result, processed >= total ? "Completed" : "Running");
        }

        if (total == 0)
            ReportSyncProgress(progress, "linenAndTowelFee", total, processed, result, "Completed");
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
            () => CreateJournalEntryFromBillPaymentWithResultAsync(paymentApplication, currentUser),
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

    private async Task<JournalEntrySyncResult> ClearJournalEntriesBySourceTypesAsync(Guid organizationId, string officeIds, params int[] sourceTypeIds)
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

    private async Task TrackJournalEntryCreateAsync(Func<Task<AccountingJournalEntryResult>> createJournalEntry, JournalEntryGetCriteria existingCriteria, JournalEntrySyncResult result)
    {
        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(existingCriteria);
        if (existingEntries.Any())
        {
            result.JournalEntriesSkipped++;
            return;
        }

        var createResult = await createJournalEntry();
        if (createResult.JournalEntry != null)
            result.JournalEntriesCreated++;

        if (createResult.HasWarning)
            result.Errors.Add(createResult.Warning!);
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

    private static void ReportSyncProgress(
        IProgress<JournalEntrySyncProgress>? progress,
        string syncType,
        int total,
        int processed,
        JournalEntrySyncResult result,
        string status)
    {
        progress?.Report(new JournalEntrySyncProgress
        {
            SyncType = syncType,
            Total = total,
            Processed = processed,
            Skipped = result.JournalEntriesSkipped,
            Errors = result.Errors.Count,
            Status = status
        });
    }

    private static List<DateOnly> GetMonthlyRunDatesForCurrentYear(DateOnly asOfDate)
    {
        var dates = new List<DateOnly>();
        for (var month = 1; month <= asOfDate.Month; month++)
            dates.Add(new DateOnly(asOfDate.Year, month, 1));

        return dates;
    }
}

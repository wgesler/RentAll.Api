using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<CloseOwnerStatementMonthResult> CloseOwnerStatementMonthAsync(
        Guid organizationId,
        DateOnly endDate,
        IReadOnlyList<OwnerStatementMonthCloseLine> lines,
        Guid currentUser)
    {
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            throw new Exception("Accounting is not enabled for this organization.");

        if (endDate == default)
            throw new Exception("End date is required to close an owner statement month.");

        if (lines == null || lines.Count == 0)
            throw new Exception("At least one owner statement line is required to close the month.");

        return new CloseOwnerStatementMonthResult
        {
            PropertiesProcessed = lines.Count
        };
    }

    public async Task<CloseAccountingPeriodResult> SoftCloseOwnerApJournalEntriesForOwnerStatementMonthAsync(
        Guid organizationId,
        DateOnly? startDate,
        DateOnly endDate,
        IReadOnlyList<OwnerStatementMonthCloseLine> lines,
        Guid currentUser)
    {
        var result = new CloseAccountingPeriodResult();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return result;

        if (endDate == default)
            throw new Exception("End date is required to soft close owner statement month activity.");

        if (lines == null || lines.Count == 0)
            return result;

        var periodStart = startDate ?? new DateOnly(endDate.Year, endDate.Month, 1);
        var targets = await ResolveOwnerStatementMonthSoftCloseTargetsAsync(organizationId, periodStart, endDate, lines);

        foreach (var journalEntryId in targets.OwnerExpectedJournalEntryIds)
            await TrySoftCloseOwnerStatementJournalEntryAsync(journalEntryId, organizationId, currentUser, result);

        foreach (var journalEntryId in targets.OwnerActualJournalEntryIds)
            await TrySoftCloseOwnerStatementJournalEntryAsync(journalEntryId, organizationId, currentUser, result);

        foreach (var (officeId, paymentId) in targets.InvoicePaymentDocuments)
            await TrySoftCloseOwnerStatementInvoicePaymentDocumentAsync(organizationId, officeId, paymentId, currentUser, result);

        foreach (var (officeId, receiptId) in targets.BillReceiptDocuments)
            await TrySoftCloseOwnerStatementBillReceiptDocumentAsync(organizationId, officeId, receiptId, currentUser, result);

        foreach (var (officeId, paymentId) in targets.OwnerPaymentDocuments)
            await TrySoftCloseOwnerStatementOwnerPaymentDocumentAsync(organizationId, officeId, paymentId, currentUser, result);

        foreach (var (officeId, workOrderId) in targets.WorkOrderDocuments)
            await TrySoftCloseOwnerStatementWorkOrderDocumentAsync(organizationId, officeId, workOrderId, currentUser, result);

        foreach (var journalEntryId in targets.LinenTowelJournalEntryIds)
            await TrySoftCloseOwnerStatementJournalEntryAsync(journalEntryId, organizationId, currentUser, result);

        return result;
    }

    private async Task<OwnerStatementMonthSoftCloseTargets> ResolveOwnerStatementMonthSoftCloseTargetsAsync(
        Guid organizationId,
        DateOnly periodStart,
        DateOnly endDate,
        IReadOnlyList<OwnerStatementMonthCloseLine> lines)
    {
        var targets = new OwnerStatementMonthSoftCloseTargets();
        var closedPropertyKeys = lines
            .Where(line => line.PropertyId != Guid.Empty && line.OfficeId > 0)
            .Select(line => GetOwnerStatementPropertyKey(line.OfficeId, line.PropertyId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (closedPropertyKeys.Count == 0)
            return targets;

        var officeIds = lines.Select(line => line.OfficeId).Where(officeId => officeId > 0).Distinct().ToList();
        foreach (var officeId in officeIds)
        {
            var linesForOffice = (await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeId.ToString(),
                StartDate = periodStart,
                EndDate = endDate,
                IncludeUnposted = true,
                IncludeCashOnly = true
            })).ToList();

            foreach (var journalEntryGroup in linesForOffice
                         .Where(line => line.PropertyId is { } propertyId
                             && propertyId != Guid.Empty
                             && closedPropertyKeys.Contains(GetOwnerStatementPropertyKey(line.OfficeId, propertyId)))
                         .GroupBy(line => line.JournalEntryId))
            {
                ClassifyOwnerStatementMonthSoftCloseTarget(journalEntryGroup.First(), periodStart, endDate, targets);
            }
        }

        return targets;
    }

    private static void ClassifyOwnerStatementMonthSoftCloseTarget(
        JournalEntryLineSearchResult line,
        DateOnly periodStart,
        DateOnly endDate,
        OwnerStatementMonthSoftCloseTargets targets)
    {
        var kind = (JournalEntryKind)line.JournalEntryKindId;
        var sourceType = line.SourceTypeId is int sourceTypeId && sourceTypeId >= 0
            ? (SourceType)sourceTypeId
            : SourceType.Journal;

        switch (kind)
        {
            case JournalEntryKind.OwnerExpected:
                if (IsAccountingPeriodInOwnerStatementCloseRange(line.AccountingPeriod, periodStart, endDate))
                    targets.OwnerExpectedJournalEntryIds.Add(line.JournalEntryId);
                return;

            case JournalEntryKind.OwnerActual:
                if (IsAccountingPeriodInOwnerStatementCloseRange(line.AccountingPeriod, periodStart, endDate))
                {
                    targets.OwnerActualJournalEntryIds.Add(line.JournalEntryId);
                    if (line.PaymentId is { } ownerActualPaymentId && ownerActualPaymentId != Guid.Empty)
                        targets.InvoicePaymentDocuments.Add((line.OfficeId, ownerActualPaymentId));
                }
                return;

            case JournalEntryKind.OwnerUtility:
            case JournalEntryKind.Bill:
            case JournalEntryKind.Receipt:
                if (IsAccountingPeriodInOwnerStatementCloseRange(line.AccountingPeriod, periodStart, endDate)
                    && line.SourceId is { } billReceiptId
                    && billReceiptId != Guid.Empty
                    && sourceType is SourceType.Bill or SourceType.Receipt or SourceType.BillPayment)
                {
                    targets.BillReceiptDocuments.Add((line.OfficeId, billReceiptId));
                }
                return;

            case JournalEntryKind.LinenTowelFee:
            case JournalEntryKind.LinenTowelUnusedReversal:
                if (IsAccountingPeriodInOwnerStatementCloseRange(line.AccountingPeriod, periodStart, endDate)
                    || IsTransactionDateInOwnerStatementCloseRange(line.TransactionDate, periodStart, endDate))
                {
                    targets.LinenTowelJournalEntryIds.Add(line.JournalEntryId);
                }
                return;

            case JournalEntryKind.BillPayment:
                if (sourceType == SourceType.OwnerDistribution
                    && line.SourceId is { } ownerPaymentId
                    && ownerPaymentId != Guid.Empty
                    && IsTransactionDateInOwnerStatementCloseRange(line.TransactionDate, periodStart, endDate))
                {
                    targets.OwnerPaymentDocuments.Add((line.OfficeId, ownerPaymentId));
                    return;
                }

                if (IsAccountingPeriodInOwnerStatementCloseRange(line.AccountingPeriod, periodStart, endDate)
                    && line.SourceId is { } receiptPaymentId
                    && receiptPaymentId != Guid.Empty
                    && sourceType is SourceType.Bill or SourceType.Receipt or SourceType.BillPayment)
                {
                    targets.BillReceiptDocuments.Add((line.OfficeId, receiptPaymentId));
                }
                return;

            case JournalEntryKind.OwnerTransfer:
            case JournalEntryKind.Expense:
                if (sourceType == SourceType.WorkOrder
                    && line.SourceId is { } workOrderId
                    && workOrderId != Guid.Empty
                    && IsAccountingPeriodInOwnerStatementCloseRange(line.AccountingPeriod, periodStart, endDate))
                {
                    targets.WorkOrderDocuments.Add((line.OfficeId, workOrderId));
                }
                return;
        }
    }

    private async Task TrySoftCloseOwnerStatementJournalEntryAsync(
        Guid journalEntryId,
        Guid organizationId,
        Guid currentUser,
        CloseAccountingPeriodResult result)
    {
        try
        {
            await SoftCloseJournalEntryIfEligibleAsync(journalEntryId, organizationId, currentUser);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            result.FailedCount++;
            result.Errors.Add(ex.Message);
        }
    }

    private async Task TrySoftCloseOwnerStatementInvoicePaymentDocumentAsync(
        Guid organizationId,
        int officeId,
        Guid paymentId,
        Guid currentUser,
        CloseAccountingPeriodResult result)
    {
        try
        {
            await SoftCloseJournalEntriesForSourceAsync(organizationId, officeId, SourceType.InvoicePayment, paymentId, organizationId, currentUser);
            await SoftCloseJournalEntriesLinkedToPaymentAsync(organizationId, paymentId, currentUser);
            await MarkDocumentSoftClosedFromReconcileCompleteAsync(SourceType.InvoicePayment, paymentId, organizationId, currentUser);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            result.FailedCount++;
            result.Errors.Add(ex.Message);
        }
    }

    private async Task TrySoftCloseOwnerStatementBillReceiptDocumentAsync(
        Guid organizationId,
        int officeId,
        Guid receiptId,
        Guid currentUser,
        CloseAccountingPeriodResult result)
    {
        try
        {
            await SoftCloseJournalEntriesForSourceAsync(organizationId, officeId, SourceType.Bill, receiptId, organizationId, currentUser);
            await SoftCloseJournalEntriesForSourceAsync(organizationId, officeId, SourceType.Receipt, receiptId, organizationId, currentUser);
            await SoftCloseJournalEntriesForSourceAsync(organizationId, officeId, SourceType.BillPayment, receiptId, organizationId, currentUser);
            await MarkDocumentSoftClosedFromReconcileCompleteAsync(SourceType.Bill, receiptId, organizationId, currentUser);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            result.FailedCount++;
            result.Errors.Add(ex.Message);
        }
    }

    private async Task TrySoftCloseOwnerStatementWorkOrderDocumentAsync(
        Guid organizationId,
        int officeId,
        Guid workOrderId,
        Guid currentUser,
        CloseAccountingPeriodResult result)
    {
        try
        {
            await SoftCloseJournalEntriesForSourceAsync(organizationId, officeId, SourceType.WorkOrder, workOrderId, organizationId, currentUser);
            await MarkWorkOrderSoftClosedForOwnerStatementAsync(workOrderId, organizationId, currentUser);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            result.FailedCount++;
            result.Errors.Add(ex.Message);
        }
    }

    private async Task MarkWorkOrderSoftClosedForOwnerStatementAsync(Guid workOrderId, Guid organizationId, Guid currentUser)
    {
        var workOrder = await _maintenanceRepository.GetWorkOrderByIdAsync(workOrderId, organizationId);
        if (workOrder == null || !CanSoftCloseDocumentFromReconcileComplete(workOrder.PostingStatusId))
            return;

        workOrder.PostingStatusId = (int)PostingStatus.SoftClosed;
        workOrder.ModifiedBy = currentUser;
        await _maintenanceRepository.UpdateWorkOrderAsync(workOrder);
    }

    private async Task TrySoftCloseOwnerStatementOwnerPaymentDocumentAsync(
        Guid organizationId,
        int officeId,
        Guid paymentId,
        Guid currentUser,
        CloseAccountingPeriodResult result)
    {
        try
        {
            await SoftCloseJournalEntriesForSourceAsync(organizationId, officeId, SourceType.OwnerDistribution, paymentId, organizationId, currentUser);
            await SoftCloseJournalEntriesLinkedToPaymentAsync(organizationId, paymentId, currentUser);
            await MarkPaymentSoftClosedFromReconcileCompleteAsync(paymentId, organizationId, currentUser);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            result.FailedCount++;
            result.Errors.Add(ex.Message);
        }
    }

    private async Task SoftCloseJournalEntriesForSourceAsync(
        Guid organizationId,
        int officeId,
        SourceType sourceType,
        Guid sourceId,
        Guid currentOrganizationId,
        Guid currentUser)
    {
        var journalEntries = await GetJournalEntriesForSourceAsync(organizationId, officeId, sourceType, sourceId);
        foreach (var journalEntry in journalEntries)
            await SoftCloseJournalEntryIfEligibleAsync(journalEntry.JournalEntryId, currentOrganizationId, currentUser);
    }

    private async Task SoftCloseJournalEntriesLinkedToPaymentAsync(Guid organizationId, Guid paymentId, Guid currentUser)
    {
        if (paymentId == Guid.Empty)
            return;

        var linkedEntries = await GetJournalEntriesByPaymentIdCachedAsync(organizationId, paymentId);
        foreach (var journalEntry in linkedEntries)
            await SoftCloseJournalEntryIfEligibleAsync(journalEntry.JournalEntryId, organizationId, currentUser);
    }

    private static bool IsAccountingPeriodInOwnerStatementCloseRange(DateOnly accountingPeriod, DateOnly periodStart, DateOnly endDate)
    {
        if (accountingPeriod == default)
            return false;

        var periodMonth = new DateOnly(accountingPeriod.Year, accountingPeriod.Month, 1);
        var rangeStartMonth = new DateOnly(periodStart.Year, periodStart.Month, 1);
        var rangeEndMonth = new DateOnly(endDate.Year, endDate.Month, 1);
        return periodMonth >= rangeStartMonth && periodMonth <= rangeEndMonth;
    }

    private static bool IsTransactionDateInOwnerStatementCloseRange(DateOnly transactionDate, DateOnly periodStart, DateOnly endDate)
        => transactionDate >= periodStart && transactionDate <= endDate;

    private static string GetOwnerStatementPropertyKey(int officeId, Guid propertyId)
        => $"{officeId:D}|{propertyId:D}";

    private sealed class OwnerStatementMonthSoftCloseTargets
    {
        public HashSet<Guid> OwnerExpectedJournalEntryIds { get; } = [];
        public HashSet<Guid> OwnerActualJournalEntryIds { get; } = [];
        public HashSet<Guid> LinenTowelJournalEntryIds { get; } = [];
        public HashSet<(int OfficeId, Guid PaymentId)> InvoicePaymentDocuments { get; } = [];
        public HashSet<(int OfficeId, Guid ReceiptId)> BillReceiptDocuments { get; } = [];
        public HashSet<(int OfficeId, Guid PaymentId)> OwnerPaymentDocuments { get; } = [];
        public HashSet<(int OfficeId, Guid WorkOrderId)> WorkOrderDocuments { get; } = [];
    }
}

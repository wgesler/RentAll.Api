using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<CloseAccountingPeriodResult> CloseJournalEntriesThroughClosedPeriodAsync(Guid organizationId, int officeId, int closedMonth, int closedYear, int startMonth, int startYear, PostingStatus closeStatus, Guid currentUser)
    {
        if (closeStatus is not (PostingStatus.SoftClosed or PostingStatus.HardClosed))
            throw new ArgumentException("Close status must be SoftClosed or HardClosed", nameof(closeStatus));

        var result = new CloseAccountingPeriodResult();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return result;

        if (closedMonth < 1 || closedMonth > 12)
            throw new Exception("ClosedMonth must be between 1 and 12.");

        if (closedYear < 1900 || closedYear > 2500)
            throw new Exception("ClosedYear must be between 1900 and 2500.");

        if (startMonth < 1 || startMonth > 12)
            throw new Exception("StartMonth must be between 1 and 12.");

        if (startYear < 1900 || startYear > 2500)
            throw new Exception("StartYear must be between 1900 and 2500.");

        var journalEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeId.ToString(),
            IncludeUnposted = true
        })).ToList();

        var eligibleEntries = journalEntries
            .Where(entry => IsAccountingPeriodOnOrBeforeClosedPeriod(entry, closedYear, closedMonth))
            .Where(entry => IsJournalEntryEligibleForAccountingOfficeClose(entry, closeStatus))
            .ToList();

        foreach (var journalEntry in eligibleEntries)
        {
            try
            {
                if (closeStatus == PostingStatus.HardClosed)
                    await HardCloseJournalEntryIfEligibleAsync(journalEntry.JournalEntryId, organizationId, currentUser);
                else
                    await SoftCloseJournalEntryIfEligibleAsync(journalEntry.JournalEntryId, organizationId, currentUser);

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(ex.Message);
            }
        }

        var documentKeys = new HashSet<(SourceType SourceType, Guid SourceId)>();
        foreach (var journalEntry in eligibleEntries)
        {
            if (journalEntry.SourceId is not Guid sourceId || sourceId == Guid.Empty)
                continue;
            if (journalEntry.SourceTypeId is not int sourceTypeId || sourceTypeId < 0)
                continue;

            documentKeys.Add(((SourceType)sourceTypeId, sourceId));
        }

        foreach (var (sourceType, sourceId) in documentKeys)
        {
            try
            {
                await TryCloseDocumentForAccountingOfficeCloseAsync(sourceType, sourceId, organizationId, currentUser, closeStatus);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(ex.Message);
            }
        }

        var startDate = new DateOnly(startYear, startMonth, 1);
        var endDate = new DateOnly(closedYear, closedMonth, DateTime.DaysInMonth(closedYear, closedMonth));
        var closedDate = await _accountingRepository.CreateClosedDateAsync(new ClosedDate
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            StartDate = startDate,
            EndDate = endDate,
            PostingStatusId = closeStatus
        });
        result.ClosedDateId = closedDate.ClosedDateId;

        return result;
    }

    private static bool IsJournalEntryEligibleForAccountingOfficeClose(JournalEntry journalEntry, PostingStatus closeStatus)
    {
        return closeStatus == PostingStatus.HardClosed
            ? journalEntry.PostingStatusId is PostingStatus.Open or PostingStatus.Posted or PostingStatus.SoftClosed
            : journalEntry.PostingStatusId is PostingStatus.Open or PostingStatus.Posted;
    }

    private static bool IsAccountingPeriodOnOrBeforeClosedPeriod(JournalEntry journalEntry, int closedYear, int closedMonth)
    {
        var accountingPeriod = journalEntry.AccountingPeriod != default
            ? FirstDayOfMonth(journalEntry.AccountingPeriod)
            : journalEntry.TransactionDate != default
                ? FirstDayOfMonth(journalEntry.TransactionDate)
                : default;

        if (accountingPeriod == default)
            return false;

        var closedPeriodOrdinal = closedYear * 12 + closedMonth;
        var entryPeriodOrdinal = accountingPeriod.Year * 12 + accountingPeriod.Month;
        return entryPeriodOrdinal <= closedPeriodOrdinal;
    }

    private async Task TryCloseDocumentForAccountingOfficeCloseAsync(SourceType sourceType, Guid sourceId, Guid organizationId, Guid currentUser, PostingStatus closeStatus)
    {
        if (closeStatus == PostingStatus.HardClosed)
        {
            switch (sourceType)
            {
                case SourceType.WorkOrder:
                    await MarkWorkOrderHardClosedForAccountingOfficeCloseAsync(sourceId, organizationId, currentUser);
                    break;
                case SourceType.OwnerDistribution:
                    await MarkPaymentHardClosedFromAccountingOfficeCloseAsync(sourceId, organizationId, currentUser);
                    break;
                default:
                    await MarkDocumentHardClosedFromAccountingOfficeCloseAsync(sourceType, sourceId, organizationId, currentUser);
                    break;
            }

            return;
        }

        switch (sourceType)
        {
            case SourceType.WorkOrder:
                await MarkWorkOrderSoftClosedForOwnerStatementAsync(sourceId, organizationId, currentUser);
                break;
            case SourceType.OwnerDistribution:
                await MarkPaymentSoftClosedFromReconcileCompleteAsync(sourceId, organizationId, currentUser);
                break;
            default:
                await MarkDocumentSoftClosedFromReconcileCompleteAsync(sourceType, sourceId, organizationId, currentUser);
                break;
        }
    }

    private async Task MarkWorkOrderHardClosedForAccountingOfficeCloseAsync(Guid workOrderId, Guid organizationId, Guid currentUser)
    {
        var workOrder = await _maintenanceRepository.GetWorkOrderByIdAsync(workOrderId, organizationId);
        if (workOrder == null || !CanHardCloseDocumentFromAccountingOfficeClose(workOrder.PostingStatusId))
            return;

        workOrder.PostingStatusId = (int)PostingStatus.HardClosed;
        workOrder.ModifiedBy = currentUser;
        await _maintenanceRepository.UpdateWorkOrderAsync(workOrder);
    }
}

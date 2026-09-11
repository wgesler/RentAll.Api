using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<CloseAccountingPeriodResult> CloseJournalEntriesThroughClosedPeriodAsync(Guid organizationId, int officeId, int softClosedMonth, int softClosedYear, int startMonth, int startYear, Guid currentUser)
    {
        var result = new CloseAccountingPeriodResult();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return result;

        if (softClosedMonth < 1 || softClosedMonth > 12)
            throw new Exception("SoftClosedMonth must be between 1 and 12.");

        if (softClosedYear < 1900 || softClosedYear > 2100)
            throw new Exception("SoftClosedYear must be between 1900 and 2100.");

        if (startMonth < 1 || startMonth > 12)
            throw new Exception("StartMonth must be between 1 and 12.");

        if (startYear < 1900 || startYear > 2100)
            throw new Exception("StartYear must be between 1900 and 2100.");

        var journalEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeId.ToString(),
            IncludeUnposted = true
        })).ToList();

        var eligibleEntries = journalEntries
            .Where(entry => IsAccountingPeriodOnOrBeforeClosedPeriod(entry, softClosedYear, softClosedMonth))
            .Where(entry => entry.PostingStatusId is PostingStatus.Open or PostingStatus.Posted)
            .ToList();

        foreach (var journalEntry in eligibleEntries)
        {
            try
            {
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
                await TrySoftCloseDocumentForAccountingOfficeCloseAsync(sourceType, sourceId, organizationId, currentUser);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(ex.Message);
            }
        }

        var startDate = new DateOnly(startYear, startMonth, 1);
        var endDate = new DateOnly(softClosedYear, softClosedMonth, DateTime.DaysInMonth(softClosedYear, softClosedMonth));
        var closedDate = await _accountingRepository.CreateClosedDateAsync(new ClosedDate
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            StartDate = startDate,
            EndDate = endDate,
            PostingStatusId = PostingStatus.SoftClosed
        });
        result.ClosedDateId = closedDate.ClosedDateId;

        return result;
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

    private async Task TrySoftCloseDocumentForAccountingOfficeCloseAsync(SourceType sourceType, Guid sourceId, Guid organizationId, Guid currentUser)
    {
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
}

using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<CloseAccountingPeriodResult> ResyncAccountingOfficePostingStatusAsync(Guid organizationId, int officeId, int softClosedMonth, int softClosedYear, int hardClosedMonth, int hardClosedYear, int startMonth, int startYear, Guid currentUser)
    {
        var result = new CloseAccountingPeriodResult();

        var softResult = await CloseJournalEntriesThroughClosedPeriodAsync(organizationId, officeId, softClosedMonth, softClosedYear, startMonth, startYear, PostingStatus.SoftClosed, currentUser);
        MergeCloseAccountingPeriodResults(result, softResult);

        var hardResult = await CloseJournalEntriesThroughClosedPeriodAsync(organizationId, officeId, hardClosedMonth, hardClosedYear, startMonth, startYear, PostingStatus.HardClosed, currentUser);
        MergeCloseAccountingPeriodResults(result, hardResult);

        var softClosedEndDate = GetAccountingOfficeClosedEndDate(softClosedYear, softClosedMonth);
        var softReopenResult = await ReopenSoftClosedPostingStatusAfterClosedEndDateAsync(organizationId, officeId, softClosedEndDate, currentUser);
        MergeCloseAccountingPeriodResults(result, softReopenResult);

        return result;
    }

    private async Task<CloseAccountingPeriodResult> ReopenSoftClosedPostingStatusAfterClosedEndDateAsync(Guid organizationId, int officeId, DateOnly softClosedEndDate, Guid currentUser)
    {
        var result = new CloseAccountingPeriodResult();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return result;

        try
        {
            result.SuccessCount = await _organizationRepository.ReopenSoftClosedPostingStatusAfterClosedEndDateAsync(organizationId, officeId, softClosedEndDate, currentUser);
        }
        catch (Exception ex)
        {
            result.FailedCount++;
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<CloseAccountingPeriodResult> ReopenHardClosedPostingStatusAfterClosedEndDateAsync(Guid organizationId, int officeId, int hardClosedMonth, int hardClosedYear, Guid currentUser)
    {
        var result = new CloseAccountingPeriodResult();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return result;

        if (hardClosedMonth < 1 || hardClosedMonth > 12)
            throw new Exception("HardClosedMonth must be between 1 and 12.");

        if (hardClosedYear < 1900 || hardClosedYear > 2500)
            throw new Exception("HardClosedYear must be between 1900 and 2500.");

        var hardClosedEndDate = GetAccountingOfficeClosedEndDate(hardClosedYear, hardClosedMonth);

        try
        {
            result.SuccessCount = await _organizationRepository.ReopenHardClosedPostingStatusAfterClosedEndDateAsync(organizationId, officeId, hardClosedEndDate, currentUser);
        }
        catch (Exception ex)
        {
            result.FailedCount++;
            result.Errors.Add(ex.Message);
        }

        return result;
    }

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

        var closedEndDate = GetAccountingOfficeClosedEndDate(closedYear, closedMonth);

        try
        {
            result.SuccessCount = await _organizationRepository.ClosePostingStatusThroughClosedEndDateAsync(organizationId, officeId, closedEndDate, (int)closeStatus, currentUser);
        }
        catch (Exception ex)
        {
            result.FailedCount++;
            result.Errors.Add(ex.Message);
        }

        var startDate = new DateOnly(startYear, startMonth, 1);
        var closedDate = await _accountingRepository.CreateClosedDateAsync(new ClosedDate
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            StartDate = startDate,
            EndDate = closedEndDate,
            PostingStatusId = closeStatus
        });
        result.ClosedDateId = closedDate.ClosedDateId;

        return result;
    }

    private static void MergeCloseAccountingPeriodResults(CloseAccountingPeriodResult target, CloseAccountingPeriodResult source)
    {
        target.SuccessCount += source.SuccessCount;
        target.FailedCount += source.FailedCount;
        target.Errors.AddRange(source.Errors);
        if (source.ClosedDateId is > 0)
            target.ClosedDateId = source.ClosedDateId;
    }

    internal static DateOnly GetAccountingOfficeClosedEndDate(int closedYear, int closedMonth)
        => new(closedYear, closedMonth, DateTime.DaysInMonth(closedYear, closedMonth));

    internal static bool IsJournalEntryInAccountingOfficeClosePeriod(JournalEntry journalEntry, DateOnly closedEndDate)
        => IsDocumentInAccountingOfficeClosePeriod(journalEntry.TransactionDate, journalEntry.AccountingPeriod, closedEndDate);

    internal static bool IsDocumentInAccountingOfficeClosePeriod(DateOnly transactionDate, DateOnly accountingPeriod, DateOnly closedEndDate)
    {
        var closedEndMonth = new DateOnly(closedEndDate.Year, closedEndDate.Month, 1);
        if (accountingPeriod != default)
        {
            var periodMonth = FirstDayOfMonth(accountingPeriod);
            if (periodMonth > closedEndMonth)
                return false;
        }

        if (transactionDate != default && transactionDate <= closedEndDate)
            return true;

        if (accountingPeriod == default)
            return false;

        return FirstDayOfMonth(accountingPeriod) <= closedEndMonth;
    }

    private async Task ApplyAccountingOfficeClosedPostingStatusComplianceAsync(JournalEntry journalEntry)
    {
        if (journalEntry.OfficeId <= 0)
            return;

        var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(journalEntry.OrganizationId, journalEntry.OfficeId);
        if (accountingOffice == null)
            return;

        EnsureJournalEntryAccountingPeriodForCloseEvaluation(journalEntry);

        var requiredStatus = ResolveRequiredPostingStatusForAccountingOfficeClose(journalEntry, accountingOffice);
        if (requiredStatus == null)
            return;

        if (requiredStatus == PostingStatus.HardClosed)
        {
            journalEntry.PostingStatusId = PostingStatus.HardClosed;
            return;
        }

        if (journalEntry.PostingStatusId != PostingStatus.HardClosed)
            journalEntry.PostingStatusId = PostingStatus.SoftClosed;
    }

    private static void EnsureJournalEntryAccountingPeriodForCloseEvaluation(JournalEntry journalEntry)
    {
        if (journalEntry.AccountingPeriod == default && journalEntry.TransactionDate != default)
            journalEntry.AccountingPeriod = FirstDayOfMonth(journalEntry.TransactionDate);
    }

    private static PostingStatus? ResolveRequiredPostingStatusForAccountingOfficeClose(JournalEntry journalEntry, AccountingOffice accountingOffice)
    {
        EnsureJournalEntryAccountingPeriodForCloseEvaluation(journalEntry);
        return ResolveRequiredPostingStatusForAccountingOfficeClose(journalEntry.TransactionDate, journalEntry.AccountingPeriod, accountingOffice);
    }

    private static PostingStatus? ResolveRequiredPostingStatusForAccountingOfficeClose(DateOnly transactionDate, DateOnly accountingPeriod, AccountingOffice accountingOffice)
    {
        var hardClosedEndDate = GetAccountingOfficeClosedEndDate(accountingOffice.HardClosedYear, accountingOffice.HardClosedMonth);
        if (IsDocumentInAccountingOfficeClosePeriod(transactionDate, accountingPeriod, hardClosedEndDate))
            return PostingStatus.HardClosed;

        var softClosedEndDate = GetAccountingOfficeClosedEndDate(accountingOffice.SoftClosedYear, accountingOffice.SoftClosedMonth);
        if (IsDocumentInAccountingOfficeClosePeriod(transactionDate, accountingPeriod, softClosedEndDate))
            return PostingStatus.SoftClosed;

        return null;
    }
}

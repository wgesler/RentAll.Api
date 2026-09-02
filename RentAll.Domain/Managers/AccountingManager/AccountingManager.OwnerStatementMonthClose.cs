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

        var closedMonthPeriod = FirstDayOfMonth(endDate);
        var balanceAccountingPeriod = closedMonthPeriod.AddMonths(1);
        var balanceTransactionDate = balanceAccountingPeriod;
        var result = new CloseOwnerStatementMonthResult();

        foreach (var line in lines)
        {
            var memo = BuildOwnerStartingBalanceMemo(line.PropertyCode, balanceAccountingPeriod);
            var existingJournalEntryId = await _accountingRepository.FindOwnerBalanceJournalEntryIdByMemoAsync(
                organizationId,
                line.OfficeId,
                line.PropertyId,
                memo);

            var staleMemo = BuildOwnerStartingBalanceMemo(line.PropertyCode, closedMonthPeriod);
            if (!string.Equals(staleMemo, memo, StringComparison.Ordinal))
            {
                var staleJournalEntryId = await _accountingRepository.FindOwnerBalanceJournalEntryIdByMemoAsync(
                    organizationId,
                    line.OfficeId,
                    line.PropertyId,
                    staleMemo);
                if (staleJournalEntryId.HasValue
                    && staleJournalEntryId != existingJournalEntryId)
                {
                    await DeleteJournalEntryAsync(staleJournalEntryId.Value, organizationId);
                }
            }

            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, line.OfficeId);
            await UpsertOwnerStartingBalanceJournalEntryAsync(
                organizationId,
                line,
                balanceAccountingPeriod,
                balanceTransactionDate,
                line.ClosingBalance,
                existingJournalEntryId,
                chartOfAccounts,
                accountingOffice,
                currentUser);

            if (existingJournalEntryId == null)
                result.JournalEntriesCreated++;
            else
                result.JournalEntriesUpdated++;

            result.PropertiesProcessed++;
        }

        return result;
    }

    private async Task<Guid> UpsertOwnerStartingBalanceJournalEntryAsync(
        Guid organizationId,
        OwnerStatementMonthCloseLine row,
        DateOnly accountingPeriod,
        DateOnly transactionDate,
        decimal closingBalance,
        Guid? existingJournalEntryId,
        List<ChartOfAccount> chartOfAccounts,
        AccountingOffice? accountingOffice,
        Guid currentUser)
    {
        var memo = BuildOwnerStartingBalanceMemo(row.PropertyCode, accountingPeriod);
        var ownerApAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, row.OfficeId, accountingOffice);
        var offsetAccountId = GetDefaultRetainedEarningsAccount(chartOfAccounts, row.OfficeId, accountingOffice);
        if (offsetAccountId <= 0)
            throw new Exception($"Retained earnings account is required for office {row.OfficeId}.");

        var amount = Math.Abs(closingBalance);
        var lineContext = new JournalEntryLineContext(
            row.PropertyId,
            row.PropertyCode,
            null,
            null,
            row.OwnerId,
            row.OwnerNameLine);

        var ownerApLine = new JournalEntryLine
        {
            ChartOfAccountId = ownerApAccountId,
            Debit = closingBalance < 0 ? amount : 0,
            Credit = closingBalance > 0 ? amount : 0,
            Memo = memo,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };
        ApplyJournalEntryLineContext(ownerApLine, lineContext);

        var offsetLine = new JournalEntryLine
        {
            ChartOfAccountId = offsetAccountId,
            Debit = closingBalance > 0 ? amount : 0,
            Credit = closingBalance < 0 ? amount : 0,
            Memo = memo,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };
        ApplyJournalEntryLineContext(offsetLine, lineContext);

        if (existingJournalEntryId is { } journalEntryId && journalEntryId != Guid.Empty)
        {
            var existing = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, organizationId)
                ?? throw new Exception($"Owner balance journal entry {journalEntryId} was not found.");

            existing.TransactionDate = transactionDate;
            existing.AccountingPeriod = accountingPeriod;
            existing.Memo = memo;
            existing.JournalEntryLines = new List<JournalEntryLine> { ownerApLine, offsetLine };
            existing.ModifiedBy = currentUser;

            var updated = await UpdateJournalEntryAsync(existing);
            return updated.JournalEntryId;
        }

        var journalEntry = ClassifyJournalEntry(new JournalEntry
        {
            OrganizationId = organizationId,
            OfficeId = row.OfficeId,
            TransactionDate = transactionDate,
            AccountingPeriod = accountingPeriod,
            SourceTypeId = (int)SourceType.Journal,
            SourceId = row.PropertyId,
            SourceCode = row.PropertyCode,
            Memo = memo,
            JournalEntryLines = new List<JournalEntryLine> { ownerApLine, offsetLine },
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        }, JournalEntryKind.Manual, Perspective.System);

        var created = await CreateJournalEntryAsync(journalEntry)
            ?? throw new Exception($"Unable to create owner balance journal entry for property {row.PropertyCode}.");

        var posted = await PostJournalEntryAsync(created.JournalEntryId, organizationId, currentUser, accountingPeriod);
        return posted.JournalEntryId;
    }
}

using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<CloseOwnerStatementMonthResult> CloseOwnerStatementMonthAsync(
        Guid organizationId,
        string officeIds,
        DateOnly endDate,
        IReadOnlyList<OwnerCashReportRow> rows,
        Guid currentUser)
    {
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            throw new Exception("Accounting is not enabled for this organization.");

        if (endDate == default)
            throw new Exception("End date is required to close an owner statement month.");

        var accountingPeriod = FirstDayOfMonth(endDate);
        var result = new CloseOwnerStatementMonthResult();

        foreach (var row in rows)
        {
            var memo = BuildOwnerStartingBalanceMemo(row.PropertyCode, accountingPeriod);
            var existingJournalEntryId = await _accountingRepository.FindOwnerBalanceJournalEntryIdByMemoAsync(
                organizationId,
                row.OfficeId,
                row.PropertyId,
                memo);

            var ledgerRows = await _accountingRepository.GetOwnerStatementPropertyLedgersAsync(
                organizationId,
                officeIds,
                endDate,
                row.PropertyId,
                existingJournalEntryId);
            var ledgerBalance = ledgerRows.FirstOrDefault()?.LedgerBalance ?? 0m;

            if (Math.Abs(ledgerBalance) >= 0.005m)
            {
                var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, row.OfficeId);
                await UpsertOwnerStartingBalanceJournalEntryAsync(
                    organizationId,
                    row,
                    accountingPeriod,
                    endDate,
                    ledgerBalance,
                    existingJournalEntryId,
                    chartOfAccounts,
                    accountingOffice,
                    currentUser);

                if (existingJournalEntryId == null)
                    result.JournalEntriesCreated++;
                else
                    result.JournalEntriesUpdated++;
            }
            else if (existingJournalEntryId.HasValue)
            {
                await DeleteJournalEntryAsync(existingJournalEntryId.Value, organizationId);
                result.JournalEntriesUpdated++;
            }

            result.PropertiesProcessed++;
        }

        return result;
    }

    private async Task<Guid> UpsertOwnerStartingBalanceJournalEntryAsync(
        Guid organizationId,
        OwnerCashReportRow row,
        DateOnly accountingPeriod,
        DateOnly transactionDate,
        decimal ledgerBalance,
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

        var amount = Math.Abs(ledgerBalance);
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
            Debit = ledgerBalance < 0 ? amount : 0,
            Credit = ledgerBalance > 0 ? amount : 0,
            Memo = memo,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };
        ApplyJournalEntryLineContext(ownerApLine, lineContext);

        var offsetLine = new JournalEntryLine
        {
            ChartOfAccountId = offsetAccountId,
            Debit = ledgerBalance > 0 ? amount : 0,
            Credit = ledgerBalance < 0 ? amount : 0,
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

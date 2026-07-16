using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Triggers
    public async Task<int> ProcessRetainedEarningsAsync(Guid organizationId, string officeIds, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default, bool logDecisions = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return 0;

        var accountingOffices = (await _organizationRepository.GetAccountingOfficesByOfficeIdsAsync(organizationId, officeIds)).ToList();
        if (accountingOffices.Count == 0)
            return 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!startDate.HasValue && !endDate.HasValue)
        {
            var dueOffices = accountingOffices.Where(o => IsRetainedEarningsDueOnDate(o, today)).ToList();
            if (logDecisions)
            {
                await LogRetainedEarningsRunAsync(organizationId, ResolveFirstOfficeIdFromCsv(officeIds), today, dueOffices.Count, dueOffices.Count == 0 ? "No accounting offices with day-after year-end today" : "Processing accounting offices on day after year-end");
            }

            await CreateJournalEntriesForRetainedEarningsAsync(organizationId, dueOffices, today, cancellationToken, logDecisions);
            return dueOffices.Count;
        }

        var processingDates = ResolveRetainedEarningsSyncProcessingDatesInRange(accountingOffices, startDate, endDate);
        var processed = 0;
        foreach (var runDate in processingDates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dueOffices = accountingOffices.Where(o => IsRetainedEarningsDueOnDate(o, runDate)).ToList();
            if (logDecisions)
            {
                await LogRetainedEarningsRunAsync(organizationId, ResolveFirstOfficeIdFromCsv(officeIds), runDate, dueOffices.Count, dueOffices.Count == 0 ? "No accounting offices with day-after year-end on date" : "Processing accounting offices on day after year-end");
            }

            await CreateJournalEntriesForRetainedEarningsAsync(organizationId, dueOffices, runDate, cancellationToken, logDecisions);
            processed += dueOffices.Count;
        }

        return processed;
    }

    public async Task CreateJournalEntriesForRetainedEarningsAsync(Guid organizationId, IReadOnlyCollection<AccountingOffice> accountingOffices, DateOnly processingDate, CancellationToken cancellationToken, bool logDecisions = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return;

        foreach (var accountingOffice in accountingOffices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await CreateJournalEntryForRetainedEarningsAsync(organizationId, accountingOffice, processingDate, cancellationToken, logDecisions);
            }
            catch (Exception ex)
            {
                await LogAccountingErrorAsync(trigger: "RetainedEarnings", organizationId: organizationId, officeId: accountingOffice.OfficeId, sourceTypeId: (int)SourceType.Journal, sourceId: null, documentCode: $"Office-{accountingOffice.OfficeId}", accountingPeriod: processingDate, amount: null, message: ex.Message, currentUser: SystemOrganization);
            }
        }
    }

    private async Task CreateJournalEntryForRetainedEarningsAsync(Guid organizationId, AccountingOffice accountingOffice, DateOnly processingDate, CancellationToken cancellationToken, bool logDecisions = false)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsRetainedEarningsDueOnDate(accountingOffice, processingDate))
        {
            if (logDecisions)
            {
                await LogRetainedEarningsDecisionAsync(organizationId, accountingOffice.OfficeId, processingDate, amount: null, $"Skipped — retained earnings only runs on the day after accounting office year-end ({accountingOffice.YearEndMonth:00}/{accountingOffice.YearEndDay:00}).");
            }
            return;
        }

        var closedDates = await _accountingRepository.GetClosedDateByCriteriaAsync(
            organizationId,
            accountingOffice.OfficeId.ToString(),
            startDate: null,
            endDate: null,
            postingStatusId: null);

        var (chartOfAccounts, _) = await LoadAccountContextAsync(organizationId, accountingOffice.OfficeId);
        var profitLossAccounts = chartOfAccounts
            .Where(account => account.OfficeId == accountingOffice.OfficeId && IsRetainedEarningsProfitLossAccount(account))
            .ToList();
        var accountTypeById = profitLossAccounts.ToDictionary(account => account.AccountId, account => account.AccountType);
        var fiscalYearsToClose = ResolveRetainedEarningsFiscalYearsToClose(accountingOffice, processingDate);

        foreach (var (fiscalYearStart, fiscalYearEnd) in fiscalYearsToClose)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsFiscalYearHardClosed(closedDates, accountingOffice.OfficeId, fiscalYearStart, fiscalYearEnd))
            {
                if (logDecisions)
                {
                    await LogRetainedEarningsDecisionAsync(organizationId, accountingOffice.OfficeId, processingDate, amount: null, $"Skipped fiscal year {fiscalYearStart:MM/dd/yyyy}-{fiscalYearEnd:MM/dd/yyyy} — period is hard closed.");
                }
                continue;
            }

            if (await HasRetainedEarningsJournalEntryAsync(organizationId, accountingOffice.OfficeId, processingDate))
            {
                await TryRefreshRetainedEarningsJournalEntryAsync(
                    organizationId,
                    accountingOffice,
                    chartOfAccounts,
                    profitLossAccounts,
                    fiscalYearStart,
                    fiscalYearEnd,
                    processingDate,
                    logDecisions);
                continue;
            }

            var journalEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = accountingOffice.OfficeId.ToString(),
                IncludeVoided = false,
                IncludeUnposted = true,
                StartDate = fiscalYearStart,
                EndDate = fiscalYearEnd
            })).ToList();

            var accountYearBalances = CalculateAccountYearBalances(journalEntries, accountTypeById, fiscalYearStart, fiscalYearEnd);
            var netIncome = RoundRetainedEarningsAmount(accountYearBalances.Values.Sum());
            var journalEntry = BuildRetainedEarningsJournalEntryForYear(organizationId, accountingOffice, chartOfAccounts, profitLossAccounts, accountYearBalances, netIncome, processingDate);

            if (journalEntry.JournalEntryLines.Count == 0)
            {
                if (logDecisions)
                {
                    await LogRetainedEarningsDecisionAsync(organizationId, accountingOffice.OfficeId, processingDate, amount: null, $"Skipped fiscal year {fiscalYearStart:MM/dd/yyyy}-{fiscalYearEnd:MM/dd/yyyy} — no profit and loss account balances to close.");
                }
                continue;
            }

            await CreateAutoGeneratedJournalEntryAsync(journalEntry);

            if (logDecisions)
            {
                await LogRetainedEarningsDecisionAsync(organizationId, accountingOffice.OfficeId, processingDate, amount: netIncome, $"Created retained earnings journal entry for fiscal year {fiscalYearStart:MM/dd/yyyy}-{fiscalYearEnd:MM/dd/yyyy} — net income ${netIncome:0.00}, {journalEntry.JournalEntryLines.Count} line(s).");
            }
        }
    }
    #endregion

    #region Journal Entries
    private JournalEntry BuildRetainedEarningsJournalEntryForYear(Guid organizationId, AccountingOffice accountingOffice, List<ChartOfAccount> chartOfAccounts, IReadOnlyCollection<ChartOfAccount> profitLossAccounts, IReadOnlyDictionary<int, decimal> accountYearBalances, decimal netIncome, DateOnly processingDate)
    {
        // AGENT-NOTE: DO NOT TOUCH.
        // RETAINED-EARNINGS-JE-ACCOUNTS
        // Retained earnings close runs on the day after AccountingOffice year-end. It zeros P&L accounts (>= 4000)
        // for the fiscal year that just ended and posts net income into Retained Earnings for the new year.
        // Line 1 — Credit: Retained Earnings (GetDefaultRetainedEarningsAccount) for net income when net income is positive; Debit when net income is negative.
        // Lines 2+ — One line per ChartOfAccount with AccountNo >= 4000, using that account's year balance to zero the account:
        //   Income / OtherIncome (credit-normal): Debit the account when balance > 0; Credit when balance < 0.
        //   COGS / Expense / OtherExpense (debit-normal): Credit the account when balance > 0; Debit when balance < 0.
        // END RETAINED-EARNINGS-JE-ACCOUNTS

        var retainedEarningsAccountId = GetDefaultRetainedEarningsAccount(chartOfAccounts, accountingOffice.OfficeId, accountingOffice);
        var journalEntryLines = new List<JournalEntryLine>();
        var accountById = profitLossAccounts.ToDictionary(account => account.AccountId);

        foreach (var (accountId, yearBalance) in accountYearBalances.OrderBy(entry => accountById.TryGetValue(entry.Key, out var account) ? account.AccountNo : entry.Key.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            if (yearBalance == 0m || !accountById.TryGetValue(accountId, out var account))
                continue;

            var closingLine = BuildRetainedEarningsClosingLineForAccount(account, yearBalance);
            if (closingLine == null)
                continue;

            journalEntryLines.Add(closingLine);
        }

        if (journalEntryLines.Count == 0)
            return new JournalEntry { OrganizationId = organizationId, OfficeId = accountingOffice.OfficeId };

        if (netIncome > 0m)
        {
            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = retainedEarningsAccountId,
                Debit = 0m,
                Credit = netIncome,
                Memo = BuildRetainedEarningsMemo(processingDate),
                CreatedBy = SystemOrganization
            });
        }
        else if (netIncome < 0m)
        {
            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = retainedEarningsAccountId,
                Debit = Math.Abs(netIncome),
                Credit = 0m,
                Memo = BuildRetainedEarningsMemo(processingDate),
                CreatedBy = SystemOrganization
            });
        }

        return new JournalEntry
        {
            OrganizationId = organizationId,
            OfficeId = accountingOffice.OfficeId,
            TransactionDate = processingDate,
            AccountingPeriod = new DateOnly(processingDate.Year, processingDate.Month, 1),
            SourceTypeId = (int)SourceType.Journal,
            Memo = BuildRetainedEarningsMemo(processingDate),
            JournalEntryLines = journalEntryLines,
            CreatedBy = SystemOrganization
        };
    }

    private JournalEntryLine? BuildRetainedEarningsClosingLineForAccount(ChartOfAccount account, decimal yearBalance)
    {
        if (yearBalance == 0m)
            return null;

        var amount = Math.Abs(yearBalance);
        var isCreditNormal = IsCreditNormalAccountType(account.AccountType);
        var debit = isCreditNormal
            ? yearBalance > 0m ? amount : 0m
            : yearBalance < 0m ? amount : 0m;
        var credit = isCreditNormal
            ? yearBalance < 0m ? amount : 0m
            : yearBalance > 0m ? amount : 0m;

        if (debit == 0m && credit == 0m)
            return null;

        return new JournalEntryLine
        {
            ChartOfAccountId = account.AccountId,
            Debit = debit,
            Credit = credit,
            Memo = BuildRetainedEarningsAccountCloseMemo(account),
            CreatedBy = SystemOrganization
        };
    }
    #endregion

    #region Retained Earnings Adjustments
    private async Task TryRefreshRetainedEarningsAfterJournalEntryChangeAsync(JournalEntry journalEntry, JournalEntry? priorJournalEntry = null, bool logDecisions = false)
    {
        try
        {
            if (!await IsAccountingFeatureEnabledAsync(journalEntry.OrganizationId))
                return;

            await TryRefreshRetainedEarningsForJournalEntryFiscalYearAsync(journalEntry, logDecisions);

            if (priorJournalEntry != null
                && priorJournalEntry.TransactionDate != journalEntry.TransactionDate
                && !IsRetainedEarningsCloseJournalEntry(priorJournalEntry))
            {
                await TryRefreshRetainedEarningsForJournalEntryFiscalYearAsync(priorJournalEntry, logDecisions);
            }
        }
        catch (Exception ex)
        {
            await LogAccountingErrorAsync(
                trigger: "RetainedEarnings",
                organizationId: journalEntry.OrganizationId,
                officeId: journalEntry.OfficeId,
                sourceTypeId: journalEntry.SourceTypeId,
                sourceId: journalEntry.SourceId,
                documentCode: journalEntry.JournalEntryCode,
                accountingPeriod: journalEntry.TransactionDate,
                amount: null,
                message: ex.Message,
                currentUser: SystemOrganization);
        }
    }

    private async Task TryRefreshRetainedEarningsForJournalEntryFiscalYearAsync(JournalEntry journalEntry, bool logDecisions)
    {
        if (IsRetainedEarningsCloseJournalEntry(journalEntry))
            return;

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(journalEntry.OrganizationId, journalEntry.OfficeId);
        if (accountingOffice == null)
            return;

        var profitLossAccounts = chartOfAccounts
            .Where(account => account.OfficeId == journalEntry.OfficeId && IsRetainedEarningsProfitLossAccount(account))
            .ToList();
        var profitLossAccountIds = profitLossAccounts.Select(account => account.AccountId).ToHashSet();
        var affectsProfitLoss = journalEntry.JournalEntryLines.Any(line =>
            (line.Debit != 0 || line.Credit != 0) && profitLossAccountIds.Contains(line.ChartOfAccountId));

        if (!affectsProfitLoss)
            return;

        var fiscalYearRange = ResolveFiscalYearRangeForTransactionDate(accountingOffice, journalEntry.TransactionDate);
        if (!fiscalYearRange.HasValue)
            return;

        var (fiscalYearStart, fiscalYearEnd) = fiscalYearRange.Value;
        var processingDate = fiscalYearEnd.AddDays(1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (processingDate > today)
            return;

        if (await GetRetainedEarningsJournalEntryAsync(journalEntry.OrganizationId, journalEntry.OfficeId, processingDate) == null)
            return;

        await TryRefreshRetainedEarningsJournalEntryAsync(
            journalEntry.OrganizationId,
            accountingOffice,
            chartOfAccounts,
            profitLossAccounts,
            fiscalYearStart,
            fiscalYearEnd,
            processingDate,
            logDecisions);
    }

    private async Task TryRefreshRetainedEarningsJournalEntryAsync(
        Guid organizationId,
        AccountingOffice accountingOffice,
        List<ChartOfAccount> chartOfAccounts,
        IReadOnlyCollection<ChartOfAccount> profitLossAccounts,
        DateOnly fiscalYearStart,
        DateOnly fiscalYearEnd,
        DateOnly processingDate,
        bool logDecisions)
    {
        var closedDates = await _accountingRepository.GetClosedDateByCriteriaAsync(
            organizationId,
            accountingOffice.OfficeId.ToString(),
            startDate: null,
            endDate: null,
            postingStatusId: null);

        if (IsFiscalYearHardClosed(closedDates, accountingOffice.OfficeId, fiscalYearStart, fiscalYearEnd))
        {
            if (logDecisions)
            {
                await LogRetainedEarningsDecisionAsync(organizationId, accountingOffice.OfficeId, processingDate, amount: null, $"Skipped retained earnings refresh for fiscal year {fiscalYearStart:MM/dd/yyyy}-{fiscalYearEnd:MM/dd/yyyy} — period is hard closed.");
            }
            return;
        }

        var existingRetainedEarningsEntry = await GetRetainedEarningsJournalEntryAsync(organizationId, accountingOffice.OfficeId, processingDate);
        if (existingRetainedEarningsEntry == null)
            return;

        if (existingRetainedEarningsEntry.PostingStatusId == PostingStatus.HardClosed)
        {
            if (logDecisions)
            {
                await LogRetainedEarningsDecisionAsync(organizationId, accountingOffice.OfficeId, processingDate, amount: null, $"Skipped retained earnings refresh for fiscal year {fiscalYearStart:MM/dd/yyyy}-{fiscalYearEnd:MM/dd/yyyy} — retained earnings journal entry is hard closed.");
            }
            return;
        }

        var journalEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = accountingOffice.OfficeId.ToString(),
            IncludeVoided = false,
            IncludeUnposted = true,
            StartDate = fiscalYearStart,
            EndDate = fiscalYearEnd
        })).ToList();

        var accountTypeById = profitLossAccounts.ToDictionary(account => account.AccountId, account => account.AccountType);
        var accountYearBalances = CalculateAccountYearBalances(journalEntries, accountTypeById, fiscalYearStart, fiscalYearEnd);
        var netIncome = RoundRetainedEarningsAmount(accountYearBalances.Values.Sum());
        var rebuiltJournalEntry = BuildRetainedEarningsJournalEntryForYear(
            organizationId,
            accountingOffice,
            chartOfAccounts,
            profitLossAccounts,
            accountYearBalances,
            netIncome,
            processingDate);

        if (rebuiltJournalEntry.JournalEntryLines.Count == 0)
        {
            if (logDecisions)
            {
                await LogRetainedEarningsDecisionAsync(organizationId, accountingOffice.OfficeId, processingDate, amount: null, $"Skipped retained earnings refresh for fiscal year {fiscalYearStart:MM/dd/yyyy}-{fiscalYearEnd:MM/dd/yyyy} — no profit and loss account balances to close.");
            }
            return;
        }

        if (RetainedEarningsJournalEntryLinesMatch(existingRetainedEarningsEntry, rebuiltJournalEntry))
            return;

        var retainedEarningsAccountId = GetDefaultRetainedEarningsAccount(chartOfAccounts, accountingOffice.OfficeId, accountingOffice);
        ApplyRetainedEarningsJournalEntryUpdate(existingRetainedEarningsEntry, rebuiltJournalEntry, retainedEarningsAccountId);
        await UpdateJournalEntryWithoutRetainedEarningsRefreshAsync(
            rebuiltJournalEntry,
            requireActiveLines: HasActiveJournalEntryLines(rebuiltJournalEntry));

        if (logDecisions)
        {
            await LogRetainedEarningsDecisionAsync(
                organizationId,
                accountingOffice.OfficeId,
                processingDate,
                amount: netIncome,
                $"Updated retained earnings journal entry for fiscal year {fiscalYearStart:MM/dd/yyyy}-{fiscalYearEnd:MM/dd/yyyy} — net income ${netIncome:0.00}, {rebuiltJournalEntry.JournalEntryLines.Count} line(s).");
        }
    }

    private static bool IsRetainedEarningsCloseJournalEntry(JournalEntry journalEntry)
        => journalEntry.SourceTypeId == (int)SourceType.Journal
            && !string.IsNullOrWhiteSpace(journalEntry.Memo)
            && journalEntry.Memo.Trim().StartsWith("Retained Earnings for ", StringComparison.OrdinalIgnoreCase);

    private static (DateOnly FiscalYearStart, DateOnly FiscalYearEnd)? ResolveFiscalYearRangeForTransactionDate(AccountingOffice accountingOffice, DateOnly transactionDate)
    {
        var yearEndThisCalendarYear = new DateOnly(transactionDate.Year, accountingOffice.YearEndMonth, accountingOffice.YearEndDay);
        var fiscalYearEnd = transactionDate <= yearEndThisCalendarYear
            ? yearEndThisCalendarYear
            : new DateOnly(transactionDate.Year + 1, accountingOffice.YearEndMonth, accountingOffice.YearEndDay);

        return ResolveFiscalYearRangeForYearEnd(fiscalYearEnd);
    }

    private async Task<JournalEntry?> GetRetainedEarningsJournalEntryAsync(Guid organizationId, int officeId, DateOnly processingDate)
    {
        var existingEntries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeId.ToString(),
            SourceTypeId = (int)SourceType.Journal,
            IncludeVoided = true,
            IncludeUnposted = true,
            StartDate = processingDate,
            EndDate = processingDate
        });

        var expectedMemo = BuildRetainedEarningsMemo(processingDate);
        return existingEntries.FirstOrDefault(entry => string.Equals(entry.Memo?.Trim(), expectedMemo, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyRetainedEarningsJournalEntryUpdate(JournalEntry existing, JournalEntry rebuilt, int retainedEarningsAccountId)
    {
        var existingRetainedEarningsLine = existing.JournalEntryLines.FirstOrDefault(line => line.ChartOfAccountId == retainedEarningsAccountId);
        var existingLineIdByAccountId = existing.JournalEntryLines
            .Where(line => line.ChartOfAccountId != retainedEarningsAccountId)
            .GroupBy(line => line.ChartOfAccountId)
            .ToDictionary(group => group.Key, group => group.First().JournalEntryLineId);

        foreach (var line in rebuilt.JournalEntryLines)
        {
            line.JournalEntryId = existing.JournalEntryId;
            if (line.ChartOfAccountId == retainedEarningsAccountId && existingRetainedEarningsLine != null)
                line.JournalEntryLineId = existingRetainedEarningsLine.JournalEntryLineId;
            else if (existingLineIdByAccountId.TryGetValue(line.ChartOfAccountId, out var lineId))
                line.JournalEntryLineId = lineId;
            else
                line.JournalEntryLineId = Guid.Empty;
        }

        rebuilt.JournalEntryId = existing.JournalEntryId;
        rebuilt.OrganizationId = existing.OrganizationId;
        rebuilt.OfficeId = existing.OfficeId;
        rebuilt.JournalEntryCode = existing.JournalEntryCode;
        rebuilt.TransactionDate = existing.TransactionDate;
        rebuilt.AccountingPeriod = existing.AccountingPeriod;
        rebuilt.PostingStatusId = existing.PostingStatusId;
        rebuilt.SourceTypeId = existing.SourceTypeId;
        rebuilt.SourceId = existing.SourceId;
        rebuilt.SourceCode = existing.SourceCode;
        rebuilt.IsCashOnly = existing.IsCashOnly;
        rebuilt.Memo = existing.Memo;
        rebuilt.CreatedBy = existing.CreatedBy;
        rebuilt.ModifiedBy = SystemOrganization;
    }

    private static bool RetainedEarningsJournalEntryLinesMatch(JournalEntry existing, JournalEntry rebuilt)
    {
        var existingLines = existing.JournalEntryLines
            .Where(line => line.Debit != 0 || line.Credit != 0)
            .OrderBy(line => line.ChartOfAccountId)
            .ThenBy(line => line.Debit)
            .ThenBy(line => line.Credit)
            .Select(line => (line.ChartOfAccountId, line.Debit, line.Credit))
            .ToList();
        var rebuiltLines = rebuilt.JournalEntryLines
            .Where(line => line.Debit != 0 || line.Credit != 0)
            .OrderBy(line => line.ChartOfAccountId)
            .ThenBy(line => line.Debit)
            .ThenBy(line => line.Credit)
            .Select(line => (line.ChartOfAccountId, line.Debit, line.Credit))
            .ToList();

        if (existingLines.Count != rebuiltLines.Count)
            return false;

        for (var index = 0; index < existingLines.Count; index++)
        {
            if (existingLines[index].ChartOfAccountId != rebuiltLines[index].ChartOfAccountId
                || existingLines[index].Debit != rebuiltLines[index].Debit
                || existingLines[index].Credit != rebuiltLines[index].Credit)
            {
                return false;
            }
        }

        return true;
    }
    #endregion

    #region Helpers
    private static bool IsRetainedEarningsDueOnDate(AccountingOffice accountingOffice, DateOnly processingDate)
        => ResolveAccountingOfficeYearEndDateBeforeProcessingDate(accountingOffice, processingDate).HasValue;

    private static DateOnly? ResolveAccountingOfficeYearEndDateBeforeProcessingDate(AccountingOffice accountingOffice, DateOnly processingDate)
    {
        var fiscalYearEndDate = processingDate.AddDays(-1);
        if (fiscalYearEndDate.Month != accountingOffice.YearEndMonth || fiscalYearEndDate.Day != accountingOffice.YearEndDay)
            return null;

        return fiscalYearEndDate;
    }

    private static (DateOnly FiscalYearStart, DateOnly FiscalYearEnd) ResolveFiscalYearRangeForYearEnd(DateOnly fiscalYearEndDate)
    {
        var fiscalYearStart = fiscalYearEndDate.AddYears(-1).AddDays(1);
        return (fiscalYearStart, fiscalYearEndDate);
    }

    private static IReadOnlyList<(DateOnly FiscalYearStart, DateOnly FiscalYearEnd)> ResolveRetainedEarningsFiscalYearsToClose(AccountingOffice accountingOffice, DateOnly processingDate)
    {
        var fiscalYearEndDate = ResolveAccountingOfficeYearEndDateBeforeProcessingDate(accountingOffice, processingDate);
        if (!fiscalYearEndDate.HasValue)
            return [];

        return [ResolveFiscalYearRangeForYearEnd(fiscalYearEndDate.Value)];
    }

    private static bool IsFiscalYearHardClosed(IReadOnlyCollection<ClosedDate> closedDates, int officeId, DateOnly fiscalYearStart, DateOnly fiscalYearEnd)
    {
        return closedDates.Any(closedDate =>
            closedDate.OfficeId == officeId
            && closedDate.PostingStatusId == PostingStatus.HardClosed
            && closedDate.StartDate <= fiscalYearStart
            && closedDate.EndDate >= fiscalYearEnd);
    }

    private async Task<bool> HasRetainedEarningsJournalEntryAsync(Guid organizationId, int officeId, DateOnly processingDate)
        => await GetRetainedEarningsJournalEntryAsync(organizationId, officeId, processingDate) != null;

    private static bool IsRetainedEarningsProfitLossAccount(ChartOfAccount account)
        => TryParseLeadingAccountNumber(account.AccountNo, out var accountNumber) && accountNumber >= 4000;

    private static bool TryParseLeadingAccountNumber(string accountNo, out int accountNumber)
    {
        accountNumber = 0;
        if (string.IsNullOrWhiteSpace(accountNo))
            return false;

        var match = System.Text.RegularExpressions.Regex.Match(accountNo.Trim(), @"^(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out accountNumber);
    }

    private static Dictionary<int, decimal> CalculateAccountYearBalances(IReadOnlyCollection<JournalEntry> journalEntries, IReadOnlyDictionary<int, AccountType> accountTypeById, DateOnly yearStart, DateOnly yearEnd)
    {
        var balances = accountTypeById.Keys.ToDictionary(accountId => accountId, _ => 0m);

        foreach (var journalEntry in journalEntries)
        {
            if (journalEntry.TransactionDate < yearStart || journalEntry.TransactionDate > yearEnd)
                continue;

            foreach (var line in journalEntry.JournalEntryLines)
            {
                if (!accountTypeById.TryGetValue(line.ChartOfAccountId, out var accountType))
                    continue;

                if (!balances.ContainsKey(line.ChartOfAccountId))
                    balances[line.ChartOfAccountId] = 0m;

                balances[line.ChartOfAccountId] = RoundRetainedEarningsAmount(
                    balances[line.ChartOfAccountId] + SignedProfitLossActivityAmount(accountType, line.Debit, line.Credit));
            }
        }

        return balances;
    }

    private static decimal SignedProfitLossActivityAmount(AccountType accountType, decimal debit, decimal credit)
    {
        if (IsCreditNormalAccountType(accountType))
            return RoundRetainedEarningsAmount(credit - debit);

        return RoundRetainedEarningsAmount(debit - credit);
    }

    private static bool IsCreditNormalAccountType(AccountType accountType)
        => accountType is AccountType.AccountsPayable
            or AccountType.CreditCard
            or AccountType.OtherCurrentLiability
            or AccountType.LongTermLiability
            or AccountType.Equity
            or AccountType.Income
            or AccountType.OtherIncome;

    private static decimal RoundRetainedEarningsAmount(decimal amount)
        => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static string BuildRetainedEarningsAccountCloseMemo(ChartOfAccount account)
        => $"{account.AccountNo} {account.Name}".Trim();

    private async Task<(DateOnly? StartDate, DateOnly? EndDate)> ResolveRetainedEarningsSyncDateRangeFromJournalEntriesAsync(Guid organizationId, string officeIds)
    {
        var journalEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeVoided = false,
            IncludeUnposted = true
        })).ToList();

        if (journalEntries.Count == 0)
            return (null, null);

        var orderedByTransactionDate = journalEntries
            .OrderBy(entry => entry.TransactionDate)
            .ThenBy(entry => entry.JournalEntryId)
            .ToList();

        return (orderedByTransactionDate[0].TransactionDate, orderedByTransactionDate[^1].TransactionDate);
    }

    private static List<DateOnly> ResolveRetainedEarningsSyncProcessingDatesInRange(IReadOnlyCollection<AccountingOffice> accountingOffices, DateOnly? startDate, DateOnly? endDate)
    {
        if (!startDate.HasValue && !endDate.HasValue)
            return [];

        var rangeStart = startDate ?? endDate!.Value;
        var rangeEnd = endDate ?? startDate!.Value;
        if (rangeStart > rangeEnd)
            (rangeStart, rangeEnd) = (rangeEnd, rangeStart);

        var dates = new HashSet<DateOnly>();
        for (var year = rangeStart.Year; year <= rangeEnd.Year; year++)
        {
            foreach (var accountingOffice in accountingOffices)
            {
                var fiscalYearEndDate = new DateOnly(year, accountingOffice.YearEndMonth, accountingOffice.YearEndDay);
                var processingDate = fiscalYearEndDate.AddDays(1);
                if (processingDate >= rangeStart && processingDate <= rangeEnd)
                    dates.Add(processingDate);
            }
        }

        return dates.OrderBy(d => d).ToList();
    }

    private async Task LogRetainedEarningsRunAsync(Guid organizationId, int? officeId, DateOnly processingDate, int officeCount, string message)
    {
        var fullMessage = officeCount > 0
            ? $"Retained earnings sync ({processingDate:MM/dd/yyyy}): {message} ({officeCount} office(s) on day after year-end)."
            : $"Retained earnings sync ({processingDate:MM/dd/yyyy}): {message}.";

        await LogAccountingLogAsync(new AccountingLog
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            PropertyId = null,
            Message = fullMessage
        });
    }

    private async Task LogRetainedEarningsDecisionAsync(Guid organizationId, int officeId, DateOnly processingDate, decimal? amount, string message)
    {
        var fullMessage = $"Retained earnings office {officeId} (as of {processingDate:MM/dd/yyyy}): {message}";
        await LogAccountingLogAsync(new AccountingLog
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            PropertyId = null,
            OriginalAmount = amount,
            Message = fullMessage
        });
    }
    #endregion
}

using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using System.Text.RegularExpressions;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<IEnumerable<OwnerStatementSummary>> GetOwnerStatementsAsync(OwnerStatementGetCriteria criteria)
    {
        var officeIds = ParseOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
            return Enumerable.Empty<OwnerStatementSummary>();

        var summaries = new List<OwnerStatementSummary>();
        foreach (var officeId in officeIds)
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(criteria.OrganizationId, officeId);
            var officeCriteria = new OwnerStatementGetCriteria
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = officeId.ToString(),
                PropertyId = criteria.PropertyId,
                StartDate = criteria.StartDate,
                EndDate = criteria.EndDate,
                ExpectedAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice),
                ActualAccountId = GetDefaultUndepositedFunds(chartOfAccounts, officeId, accountingOffice),
                PrePaidAccountId = GetDefaultPrePayment(chartOfAccounts, officeId, accountingOffice),
                ExpenseAccountId = GetDefaultOwnerExpense(chartOfAccounts, officeId, accountingOffice)
            };
            summaries.AddRange(await _accountingRepository.GetOwnerStatementsAsync(officeCriteria));
        }

        if (summaries.Count == 0)
            return summaries;

        foreach (var summary in summaries)
        {
            summary.Outstanding = summary.Expected - summary.Income;
            summary.Balance = summary.Income - summary.Expenses;
            summary.WorkingCapitalBalanceDue = summary.Balance - summary.WorkingCapital;
        }

        return summaries
            .OrderBy(summary => summary.OfficeName)
            .ThenBy(summary => summary.PropertyCode)
            .ToList();
    }

    public async Task<IEnumerable<OwnerStatementJournalEntryLine>> GetOwnerStatementJournalEntryLinesAsync(OwnerStatementJournalEntryLineGetCriteria criteria)
    {
        return await _accountingRepository.GetOwnerStatementJournalEntryLinesAsync(criteria);
    }

    public async Task<IEnumerable<OwnerStatementPropertyActivityLine>> GetOwnerStatementPropertyActivityLinesAsync(OwnerStatementPropertyActivityGetCriteria criteria)
    {
        if (criteria.PropertyId == Guid.Empty)
            return Enumerable.Empty<OwnerStatementPropertyActivityLine>();

        var property = await _propertyRepository.GetPropertyByIdAsync(criteria.PropertyId, criteria.OrganizationId);
        if (property == null)
            return Enumerable.Empty<OwnerStatementPropertyActivityLine>();

        var lines = new List<OwnerStatementPropertyActivityLine>();
        var invoiceLines = await GetOwnerStatementInvoiceActivityLinesFromJournalEntriesAsync(criteria);
        lines.AddRange(invoiceLines);

        var billReceiptLines = await GetOwnerStatementBillReceiptActivityLinesFromJournalEntriesAsync(criteria);
        lines.AddRange(billReceiptLines);
        var workOrderLines = await GetOwnerStatementWorkOrderActivityLinesFromJournalEntriesAsync(criteria);
        lines.AddRange(workOrderLines);

        return lines
            .OrderBy(line => line.ActivityDate)
            .ThenBy(line => line.ActivityType)
            .ThenBy(line => line.DocumentCode)
            .ToList();
    }

    private async Task<IEnumerable<OwnerStatementPropertyActivityLine>> GetOwnerStatementInvoiceActivityLinesFromJournalEntriesAsync(OwnerStatementPropertyActivityGetCriteria criteria)
    {
        var invoiceLines = new List<OwnerStatementPropertyActivityLine>();
        var officeIds = ParseOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
            return invoiceLines;

        foreach (var officeId in officeIds)
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(criteria.OrganizationId, officeId);
            var ownerAccountsPayableAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice);
            var ownerShareLines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = officeId.ToString(),
                ChartOfAccountId = ownerAccountsPayableAccountId,
                SourceTypeId = (int)SourceType.Invoice,
                PropertyId = criteria.PropertyId,
                IncludeVoided = false,
                IncludeUnposted = true,
                StartDate = criteria.StartDate,
                EndDate = criteria.EndDate
            });

            var groupedInvoiceLines = ownerShareLines
                .GroupBy(line => line.SourceId ?? line.JournalEntryId)
                .Select(group =>
                {
                    var first = group.First();
                    var expectedIncome = group.Sum(line => line.Credit - line.Debit);
                    var documentCode = ExtractInvoiceCodeFromJournalEntryMemo(first.JournalEntryMemo, first.JournalEntryCode);
                    var description = !string.IsNullOrWhiteSpace(first.JournalEntryMemo)
                        ? first.JournalEntryMemo!.Trim()
                        : documentCode;
                    return new OwnerStatementPropertyActivityLine
                    {
                        ActivityId = first.SourceId ?? first.JournalEntryId,
                        ActivityType = "Reservation",
                        ActivityDate = first.TransactionDate,
                        DocumentCode = documentCode,
                        Description = description,
                        ExpectedIncome = expectedIncome,
                        Expenses = 0m
                    };
                })
                .Where(line => line.ExpectedIncome != 0m);
            invoiceLines.AddRange(groupedInvoiceLines);
        }

        return invoiceLines;
    }

    private async Task<IEnumerable<OwnerStatementPropertyActivityLine>> GetOwnerStatementWorkOrderActivityLinesFromJournalEntriesAsync(OwnerStatementPropertyActivityGetCriteria criteria)
    {
        var workOrderLines = new List<OwnerStatementPropertyActivityLine>();
        var officeIds = ParseOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
            return workOrderLines;

        foreach (var officeId in officeIds)
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(criteria.OrganizationId, officeId);
            var ownerIncomeAccountId = GetDefaultOwnerIncome(chartOfAccounts, officeId, accountingOffice);
            var ownerExpenseAccountId = GetDefaultOwnerExpense(chartOfAccounts, officeId, accountingOffice);
            var journalEntryLines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = officeId.ToString(),
                SourceTypeId = (int)SourceType.WorkOrder,
                PropertyId = criteria.PropertyId,
                IncludeVoided = false,
                IncludeUnposted = true,
                StartDate = criteria.StartDate,
                EndDate = criteria.EndDate
            });

            var groupedWorkOrderLines = journalEntryLines
                .GroupBy(line => line.SourceId ?? line.JournalEntryId)
                .Select(group =>
                {
                    var first = group.First();
                    var income = group
                        .Where(line => line.ChartOfAccountId == ownerIncomeAccountId)
                        .Sum(line => line.Credit - line.Debit);
                    var expenses = group
                        .Where(line => line.ChartOfAccountId == ownerExpenseAccountId)
                        .Sum(line => line.Debit - line.Credit);
                    var documentCode = ExtractWorkOrderCodeFromJournalEntry(group.Select(line => line.Memo), first.JournalEntryMemo, first.JournalEntryCode);
                    var description = !string.IsNullOrWhiteSpace(first.JournalEntryMemo)
                        ? first.JournalEntryMemo!.Trim()
                        : documentCode;
                    return new OwnerStatementPropertyActivityLine
                    {
                        ActivityId = first.SourceId ?? first.JournalEntryId,
                        ActivityType = "WorkOrder",
                        ActivityDate = first.TransactionDate,
                        DocumentCode = documentCode,
                        Description = description,
                        ExpectedIncome = income,
                        Expenses = expenses
                    };
                })
                .Where(line => line.ExpectedIncome != 0m || line.Expenses != 0m);
            workOrderLines.AddRange(groupedWorkOrderLines);
        }

        return workOrderLines;
    }

    private static string ExtractInvoiceCodeFromJournalEntryMemo(string? journalEntryMemo, string journalEntryCode)
    {
        var memo = (journalEntryMemo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(memo))
            return journalEntryCode;

        if (memo.StartsWith("Owner Share - ", StringComparison.OrdinalIgnoreCase))
            return memo["Owner Share - ".Length..].Trim();

        if (memo.StartsWith("Invoice ", StringComparison.OrdinalIgnoreCase))
            return memo["Invoice ".Length..].Trim();

        return memo;
    }

    private static string ExtractWorkOrderCodeFromJournalEntry(IEnumerable<string?> lineMemos, string? journalEntryMemo, string journalEntryCode)
    {
        const string workOrderPattern = @"WO-\d+";
        var memoCandidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(journalEntryMemo))
            memoCandidates.Add(journalEntryMemo!.Trim());
        memoCandidates.AddRange(lineMemos.Where(memo => !string.IsNullOrWhiteSpace(memo)).Select(memo => memo!.Trim()));
        foreach (var candidate in memoCandidates)
        {
            var match = Regex.Match(candidate, workOrderPattern, RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Value.ToUpperInvariant();
        }

        return $"WO-{journalEntryCode}";
    }

    private static List<int> ParseOfficeIds(string officeIdsCsv)
    {
        return officeIdsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var officeId) ? officeId : 0)
            .Where(officeId => officeId > 0)
            .Distinct()
            .ToList();
    }

    private async Task<IEnumerable<OwnerStatementPropertyActivityLine>> GetOwnerStatementBillReceiptActivityLinesFromJournalEntriesAsync(OwnerStatementPropertyActivityGetCriteria criteria)
    {
        var billReceiptLineResults = new List<JournalEntryLineSearchResult>();
        foreach (var sourceType in new[] { (int)SourceType.Bill, (int)SourceType.Receipt })
        {
            var sourceLines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = criteria.OfficeIds,
                SourceTypeId = sourceType,
                PropertyId = criteria.PropertyId,
                IncludeVoided = false,
                IncludeUnposted = true,
                StartDate = criteria.StartDate,
                EndDate = criteria.EndDate
            });
            billReceiptLineResults.AddRange(sourceLines);
        }

        if (billReceiptLineResults.Count == 0)
            return Enumerable.Empty<OwnerStatementPropertyActivityLine>();

        return billReceiptLineResults
            .GroupBy(line => line.JournalEntryId)
            .Select(group =>
            {
                var first = group.First();
                var debitTotal = group.Sum(line => line.Debit);
                var creditTotal = group.Sum(line => line.Credit);
                var expenseAmount = Math.Max(debitTotal, creditTotal);
                var activityType = first.SourceTypeId == (int)SourceType.Bill ? "Bill" : "Receipt";
                var description = !string.IsNullOrWhiteSpace(first.JournalEntryMemo)
                    ? first.JournalEntryMemo!.Trim()
                    : (!string.IsNullOrWhiteSpace(first.Memo) ? first.Memo!.Trim() : first.JournalEntryCode);
                return new OwnerStatementPropertyActivityLine
                {
                    ActivityId = first.SourceId ?? first.JournalEntryId,
                    ActivityType = activityType,
                    ActivityDate = first.TransactionDate,
                    DocumentCode = first.JournalEntryCode,
                    Description = description,
                    ExpectedIncome = 0m,
                    Expenses = expenseAmount
                };
            })
            .Where(line => line.Expenses != 0m)
            .OrderBy(line => line.ActivityDate)
            .ThenBy(line => line.ActivityType)
            .ThenBy(line => line.DocumentCode)
            .ToList();
    }

}

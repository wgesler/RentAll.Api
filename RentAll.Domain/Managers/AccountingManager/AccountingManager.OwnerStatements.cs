using RentAll.Domain.Enums;
using RentAll.Domain.Models;

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
            summary.OwnerPayment = summary.Income <= 0m
                ? 0m
                : Math.Max(0m, summary.Balance - summary.WorkingCapital);
            summary.EndingBalance = summary.Balance - summary.OwnerPayment;
        }

        return summaries
            .OrderBy(summary => summary.OfficeName)
            .ThenBy(summary => summary.PropertyCode)
            .ToList();
    }

    public async Task<IEnumerable<OwnerStatementJournalEntryLine>> GetOwnerStatementJournalEntryLinesAsync(OwnerStatementJournalEntryLineGetCriteria criteria)
    {
        var officeIds = ParseOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
            return Enumerable.Empty<OwnerStatementJournalEntryLine>();

        var lines = new List<OwnerStatementJournalEntryLine>();
        foreach (var officeId in officeIds)
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(criteria.OrganizationId, officeId);
            var officeCriteria = new OwnerStatementJournalEntryLineGetCriteria
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = officeId.ToString(),
                OwnerId = criteria.OwnerId,
                PropertyId = criteria.PropertyId,
                Metric = criteria.Metric,
                StartDate = criteria.StartDate,
                EndDate = criteria.EndDate,
                ExpectedAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice),
                ActualAccountId = GetDefaultUndepositedFunds(chartOfAccounts, officeId, accountingOffice),
                PrePaidAccountId = GetDefaultPrePayment(chartOfAccounts, officeId, accountingOffice),
                ExpenseAccountId = GetDefaultOwnerExpense(chartOfAccounts, officeId, accountingOffice)
            };
            lines.AddRange(await _accountingRepository.GetOwnerStatementJournalEntryLinesAsync(officeCriteria));
        }

        return lines
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .ThenByDescending(line => line.Amount)
            .ToList();
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
            var ownerPaymentLines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = officeId.ToString(),
                ChartOfAccountId = ownerAccountsPayableAccountId,
                SourceTypeId = (int)SourceType.InvoicePayment,
                PropertyId = criteria.PropertyId,
                IncludeVoided = false,
                IncludeUnposted = true,
                StartDate = criteria.StartDate,
                EndDate = criteria.EndDate
            });
            var receivedIncomeByInvoiceCode = ownerPaymentLines
                .Select(line => new { Line = line, InvoiceCode = TryExtractInvoiceCode(line.JournalEntryMemo, line.Memo) })
                .Where(item => !string.IsNullOrWhiteSpace(item.InvoiceCode))
                .GroupBy(item => item.InvoiceCode!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(item => item.Line.Credit - item.Line.Debit),
                    StringComparer.OrdinalIgnoreCase);

            var groupedInvoiceLines = ownerShareLines
                .GroupBy(line => line.SourceId ?? line.JournalEntryId)
                .Select(group =>
                {
                    var first = group.First();
                    var expectedIncome = group.Sum(line => line.Credit - line.Debit);
                    var documentCode = first.JournalEntryCode;
                    var description = !string.IsNullOrWhiteSpace(first.JournalEntryMemo)
                        ? first.JournalEntryMemo!.Trim()
                        : documentCode;
                    var invoiceCode = TryExtractInvoiceCode(first.JournalEntryMemo, first.Memo, description);
                    var receivedIncome = !string.IsNullOrWhiteSpace(invoiceCode) && receivedIncomeByInvoiceCode.TryGetValue(invoiceCode, out var value)
                        ? value
                        : 0m;
                    return new OwnerStatementPropertyActivityLine
                    {
                        ActivityId = first.SourceId ?? first.JournalEntryId,
                        ActivityType = "Reservation",
                        ActivityDate = first.TransactionDate,
                        DocumentCode = documentCode,
                        Description = description,
                        ExpectedIncome = expectedIncome,
                        ReceivedIncome = receivedIncome,
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
                    var documentCode = first.JournalEntryCode;
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
                        ReceivedIncome = income,
                        Expenses = expenses
                    };
                })
                .Where(line => line.ExpectedIncome != 0m || line.Expenses != 0m);
            workOrderLines.AddRange(groupedWorkOrderLines);
        }

        return workOrderLines;
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

    private static string? TryExtractInvoiceCode(params string?[] values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var token = value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Trim(',', '.', ';', ':', '(', ')'))
                .FirstOrDefault(part => part.StartsWith("R-", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(token))
                return token;
        }

        return null;
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
                StartDate = null,
                EndDate = null
            });
            billReceiptLineResults.AddRange(sourceLines.Where(HasOwnerMemo));
        }

        if (billReceiptLineResults.Count == 0)
            return Enumerable.Empty<OwnerStatementPropertyActivityLine>();

        var receiptDateByReceiptId = new Dictionary<Guid, DateOnly>();
        var receiptIds = billReceiptLineResults
            .Select(line => line.SourceId)
            .Where(sourceId => sourceId.HasValue)
            .Select(sourceId => sourceId!.Value)
            .Distinct()
            .ToList();
        foreach (var receiptId in receiptIds)
        {
            var receipt = await _maintenanceRepository.GetReceiptByIdAsync(receiptId, criteria.OrganizationId);
            if (receipt != null)
                receiptDateByReceiptId[receiptId] = receipt.ReceiptDate;
        }

        return billReceiptLineResults
            .GroupBy(line => line.JournalEntryId)
            .Select(group =>
            {
                var first = group.First();
                var sourceId = first.SourceId;
                var activityDate = sourceId.HasValue && receiptDateByReceiptId.TryGetValue(sourceId.Value, out var receiptDate)
                    ? receiptDate
                    : first.TransactionDate;
                var debitTotal = group.Sum(line => line.Debit);
                var creditTotal = group.Sum(line => line.Credit);
                var expenseAmount = Math.Max(debitTotal, creditTotal);
                var activityType = first.SourceTypeId == (int)SourceType.Bill ? "Bill" : "Receipt";
                var description = group
                    .Select(line => line.JournalEntryMemo)
                    .Concat(group.Select(line => line.Memo))
                    .Where(memo => !string.IsNullOrWhiteSpace(memo))
                    .Select(memo => memo!.Trim())
                    .FirstOrDefault(memo => memo.StartsWith("Owner:", StringComparison.OrdinalIgnoreCase))
                    ?? first.JournalEntryCode;
                return new OwnerStatementPropertyActivityLine
                {
                    ActivityId = first.SourceId ?? first.JournalEntryId,
                    ActivityType = activityType,
                    ActivityDate = activityDate,
                    DocumentCode = first.JournalEntryCode,
                    Description = description,
                    ExpectedIncome = 0m,
                    ReceivedIncome = 0m,
                    Expenses = expenseAmount
                };
            })
            .Where(line => line.Expenses != 0m)
            .Where(line => IsWithinOwnerStatementDateRange(line.ActivityDate, criteria.StartDate, criteria.EndDate))
            .OrderBy(line => line.ActivityDate)
            .ThenBy(line => line.ActivityType)
            .ThenBy(line => line.DocumentCode)
            .ToList();
    }

    private static bool HasOwnerMemo(JournalEntryLineSearchResult line)
    {
        var journalMemo = (line.JournalEntryMemo ?? string.Empty).Trim();
        var lineMemo = (line.Memo ?? string.Empty).Trim();
        return journalMemo.StartsWith("Owner:", StringComparison.OrdinalIgnoreCase)
            || lineMemo.StartsWith("Owner:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWithinOwnerStatementDateRange(DateOnly activityDate, DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate.HasValue && activityDate < startDate.Value)
            return false;
        if (endDate.HasValue && activityDate > endDate.Value)
            return false;
        return true;
    }

}

using System.Globalization;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private sealed class OwnerAccrualSourceGroup
    {
        public Guid PropertyId { get; set; }
        public int OfficeId { get; set; }
        public string AccountingPeriod { get; set; } = string.Empty;
        public string SourceDocumentCode { get; set; } = string.Empty;
        public Guid? SourceId { get; set; }
        public int? SourceTypeId { get; set; }
        public string TransactionDate { get; set; } = string.Empty;
        public long SortDateValue { get; set; }
        public decimal OwnerRentValue { get; set; }
        public decimal OwnerPaymentReceivedValue { get; set; }
        public decimal OwnerExpenseValue { get; set; }
        public string OwnerRentMemo { get; set; } = string.Empty;
        public string OwnerExpenseMemo { get; set; } = string.Empty;
        public string OwnerPaymentMemo { get; set; } = string.Empty;
        public string OwnerRentJournalEntryCode { get; set; } = string.Empty;
        public string OwnerExpenseJournalEntryCode { get; set; } = string.Empty;
        public string OwnerPaymentJournalEntryCode { get; set; } = string.Empty;
        public Guid? OwnerRentJournalEntryLineId { get; set; }
        public Guid? OwnerExpenseJournalEntryLineId { get; set; }
        public Guid? OwnerPaymentJournalEntryLineId { get; set; }
        public Guid? OwnerRentSourceId { get; set; }
        public int? OwnerRentSourceTypeId { get; set; }
        public Guid? OwnerPaymentSourceId { get; set; }
        public int? OwnerPaymentSourceTypeId { get; set; }
        public Guid? OwnerExpenseSourceId { get; set; }
        public int? OwnerExpenseSourceTypeId { get; set; }
    }

    public async Task<OwnerAccrualReport> GetOwnerAccrualReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        var lines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(criteria)).ToList();
        var recapRows = BuildRecapReportRows(lines);
        var officeIds = ParseReportOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
            return new OwnerAccrualReport();

        var properties = await LoadOwnerCashPropertyReportDataAsync(criteria);
        var startingBalanceByKey = await GetOwnerCashStartingBalanceByKeyAsync(criteria, officeIds);
        var recapRowsByProperty = recapRows
            .Where(row => row.PropertyId.HasValue && row.PropertyId.Value != Guid.Empty)
            .GroupBy(row => BuildOwnerCashPropertyKey(row.OfficeId, row.PropertyId!.Value))
            .ToDictionary(
                group => group.Key,
                group => group.ToList(),
                StringComparer.OrdinalIgnoreCase);
        var propertyActivityLines = BuildOwnerAccrualPropertyActivityLines(lines);
        var activityLinesByProperty = BuildOwnerReportPropertyActivityLinesByKey(propertyActivityLines);

        var rows = properties
            .Select(property =>
            {
                var propertyKey = BuildOwnerCashPropertyKey(property.OfficeId, property.PropertyId);
                var startingBalance = ResolveOwnerCashStartingBalance(
                    startingBalanceByKey,
                    property.OfficeId,
                    property.PropertyId,
                    property.PrimaryOwnerId);
                activityLinesByProperty.TryGetValue(propertyKey, out var activityLines);
                activityLines ??= [];
                recapRowsByProperty.TryGetValue(propertyKey, out var propertyRecapRows);
                propertyRecapRows ??= [];

                var invoicedIncome = activityLines.Sum(line => line.ExpectedIncome);
                var paidIncome = activityLines.Sum(line => line.ReceivedIncome);
                var prepaidIncome = ResolvePropertyPrepaidIncome(propertyRecapRows);
                var ownerExpenses = activityLines.Sum(line => line.Expenses);
                var unpaidIncome = Math.Max(0m, invoicedIncome - paidIncome);
                var ownerProfit = activityLines.Sum(line => line.ReceivedIncome - line.Expenses);

                return new OwnerAccrualReportRow
                {
                    PropertyId = property.PropertyId,
                    OfficeId = property.OfficeId,
                    OfficeName = property.OfficeName,
                    OwnerId = property.PrimaryOwnerId,
                    PropertyCode = property.PropertyCode,
                    CompanyName = property.CompanyName,
                    OwnerNames = property.OwnerNames,
                    OwnerNameLine = property.OwnerNameLine,
                    StartingBalance = startingBalance,
                    InvoicedIncome = invoicedIncome,
                    PrepaidIncome = prepaidIncome,
                    PaidIncome = paidIncome,
                    UnpaidIncome = unpaidIncome,
                    OwnerExpenses = ownerExpenses,
                    OwnerProfit = ownerProfit
                };
            })
            .OrderBy(row => row.OfficeName)
            .ThenBy(row => row.PropertyCode)
            .ToList();

        return new OwnerAccrualReport
        {
            Rows = rows,
            PropertyActivityLines = propertyActivityLines
        };
    }

    private static List<OwnerStatementPropertyActivityLine> BuildOwnerAccrualPropertyActivityLines(IEnumerable<JournalEntryRecapLine> lines)
    {
        var groups = new Dictionary<string, OwnerAccrualSourceGroup>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines ?? [])
        {
            if (!line.PropertyId.HasValue || line.PropertyId.Value == Guid.Empty)
                continue;

            var category = (line.RecapCategory ?? string.Empty).Trim();
            if (!IsOwnerAccrualRecapCategory(category))
                continue;

            var groupKey = BuildOwnerAccrualSourceGroupKey(line, category);
            if (!groups.TryGetValue(groupKey, out var group))
            {
                group = new OwnerAccrualSourceGroup
                {
                    PropertyId = line.PropertyId.Value,
                    OfficeId = line.OfficeId,
                    AccountingPeriod = line.AccountingPeriod.ToString("yyyy-MM-dd"),
                    SourceDocumentCode = ResolveRecapSourceDocumentCode(line),
                    TransactionDate = line.TransactionDate.ToString("yyyy-MM-dd"),
                    SortDateValue = line.TransactionDate.ToDateTime(TimeOnly.MinValue).Ticks
                };
                groups[groupKey] = group;
            }

            ApplyOwnerAccrualRecapLine(group, line, category);
            TouchOwnerAccrualSourceGroupMetadata(group, line);
        }

        return groups.Values
            .Where(HasOwnerAccrualSourceGroupActivity)
            .SelectMany(ExpandOwnerAccrualSourceGroupActivityLines)
            .OrderBy(line => line.OfficeId)
            .ThenBy(line => line.PropertyId)
            .ThenBy(line => line.ActivityDate)
            .ThenBy(line => ResolveOwnerAccrualActivitySortOrder(line))
            .ThenBy(line => line.AccountingPeriod, StringComparer.Ordinal)
            .ThenBy(line => line.DocumentCode, StringComparer.Ordinal)
            .ToList();
    }

    private static int ResolveOwnerAccrualActivitySortOrder(OwnerStatementPropertyActivityLine line)
    {
        if (line.Expenses != 0 && line.ExpectedIncome == 0 && line.ReceivedIncome == 0)
            return 3;

        if (line.ExpectedIncome > line.ReceivedIncome)
            return 0;

        if (line.ExpectedIncome == 0 && line.ReceivedIncome != 0)
            return 2;

        return 1;
    }

    private static bool IsOwnerAccrualRecapCategory(string category) =>
        string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase)
        || string.Equals(category, "OwnerPayment", StringComparison.OrdinalIgnoreCase)
        || string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase);

    private static string BuildOwnerAccrualSourceGroupKey(JournalEntryRecapLine line, string category)
    {
        var propertyKey = BuildPropertyKey(line);
        var reservationKey = BuildReservationKey(line);
        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
        var sourceKey = ResolveOwnerAccrualSourceGroupKey(line);
        var categorySuffix = string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase)
            ? "|expense"
            : string.Empty;

        return $"{propertyKey}|{reservationKey}|{periodKey}|{sourceKey}{categorySuffix}";
    }

    private static string ResolveOwnerAccrualSourceGroupKey(JournalEntryRecapLine line)
    {
        var sourceDocumentCode = ResolveRecapSourceDocumentCode(line);
        if (!string.IsNullOrWhiteSpace(sourceDocumentCode))
            return sourceDocumentCode;

        if (line.SourceId.HasValue && line.SourceId.Value != Guid.Empty)
            return line.SourceId.Value.ToString("D");

        return line.JournalEntryLineId.ToString("D");
    }

    private static void ApplyOwnerAccrualRecapLine(OwnerAccrualSourceGroup group, JournalEntryRecapLine line, string category)
    {
        var amount = line.Amount;
        var memo = (line.Description ?? string.Empty).Trim();
        var journalEntryCode = (line.JournalEntryCode ?? string.Empty).Trim();

        if (string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase))
        {
            group.OwnerRentValue += amount;
            if (!string.IsNullOrWhiteSpace(memo))
                group.OwnerRentMemo = memo;
            if (!string.IsNullOrWhiteSpace(journalEntryCode))
                group.OwnerRentJournalEntryCode = journalEntryCode;
            group.OwnerRentJournalEntryLineId = line.JournalEntryLineId;
            group.OwnerRentSourceId = line.SourceId;
            group.OwnerRentSourceTypeId = line.SourceTypeId;
            return;
        }

        if (string.Equals(category, "OwnerPayment", StringComparison.OrdinalIgnoreCase))
        {
            group.OwnerPaymentReceivedValue += amount;
            if (!string.IsNullOrWhiteSpace(memo))
                group.OwnerPaymentMemo = memo;
            if (!string.IsNullOrWhiteSpace(journalEntryCode))
                group.OwnerPaymentJournalEntryCode = journalEntryCode;
            group.OwnerPaymentJournalEntryLineId = line.JournalEntryLineId;
            group.OwnerPaymentSourceId = line.SourceId;
            group.OwnerPaymentSourceTypeId = line.SourceTypeId;
            return;
        }

        group.OwnerExpenseValue += amount;
        if (!string.IsNullOrWhiteSpace(memo))
            group.OwnerExpenseMemo = memo;
        if (!string.IsNullOrWhiteSpace(journalEntryCode))
            group.OwnerExpenseJournalEntryCode = journalEntryCode;
        group.OwnerExpenseJournalEntryLineId = line.JournalEntryLineId;
        group.OwnerExpenseSourceId = line.SourceId;
        group.OwnerExpenseSourceTypeId = line.SourceTypeId;
    }

    private static void TouchOwnerAccrualSourceGroupMetadata(OwnerAccrualSourceGroup group, JournalEntryRecapLine line)
    {
        var transactionDate = line.TransactionDate.ToString("yyyy-MM-dd");
        if (string.IsNullOrWhiteSpace(transactionDate))
            return;

        if (string.IsNullOrWhiteSpace(group.TransactionDate)
            || string.CompareOrdinal(transactionDate, group.TransactionDate) < 0)
        {
            group.TransactionDate = transactionDate;
            group.SortDateValue = line.TransactionDate.ToDateTime(TimeOnly.MinValue).Ticks;
        }

        if (string.IsNullOrWhiteSpace(group.SourceDocumentCode))
            group.SourceDocumentCode = ResolveRecapSourceDocumentCode(line);

        if (!group.SourceId.HasValue && line.SourceId.HasValue)
            group.SourceId = line.SourceId;

        if (!group.SourceTypeId.HasValue && line.SourceTypeId.HasValue)
            group.SourceTypeId = line.SourceTypeId;
    }

    private static bool HasOwnerAccrualSourceGroupActivity(OwnerAccrualSourceGroup group) =>
        group.OwnerRentValue != 0
        || group.OwnerExpenseValue != 0
        || group.OwnerPaymentReceivedValue != 0;

    private static bool HasOwnerAccrualReportRecapActivity(RecapReportRow row) =>
        row.OwnerRentValue != 0
        || row.OwnerExpenseValue != 0
        || row.OwnerPaymentReceivedValue != 0;

    private static IEnumerable<OwnerStatementPropertyActivityLine> ExpandOwnerAccrualSourceGroupActivityLines(OwnerAccrualSourceGroup group)
    {
        var hasOwnerRent = group.OwnerRentValue != 0;
        var hasOwnerPaymentReceived = group.OwnerPaymentReceivedValue != 0;
        var accountingPeriod = FormatJournalEntryRecapAccountingPeriod(group.AccountingPeriod);

        if (hasOwnerRent && hasOwnerPaymentReceived)
        {
            yield return new OwnerStatementPropertyActivityLine
            {
                PropertyId = group.PropertyId,
                OfficeId = group.OfficeId,
                ActivityId = group.OwnerRentJournalEntryLineId,
                SourceId = group.OwnerRentSourceId,
                JournalEntryLineId = group.OwnerRentJournalEntryLineId,
                ActivityType = GetRecapActivityType(group.OwnerRentSourceTypeId, group.SourceDocumentCode),
                ActivityDate = ParseOwnerAccrualActivityDate(group.TransactionDate),
                AccountingPeriod = accountingPeriod,
                DocumentCode = ResolveOwnerAccrualOwnerRentDocumentCode(group),
                Description = ResolveOwnerAccrualOwnerRentDescription(group),
                ExpectedIncome = group.OwnerRentValue,
                ReceivedIncome = group.OwnerPaymentReceivedValue,
                Expenses = 0,
                OwnerPayment = 0
            };
        }
        else if (hasOwnerRent)
        {
            yield return new OwnerStatementPropertyActivityLine
            {
                PropertyId = group.PropertyId,
                OfficeId = group.OfficeId,
                ActivityId = group.OwnerRentJournalEntryLineId,
                SourceId = group.OwnerRentSourceId,
                JournalEntryLineId = group.OwnerRentJournalEntryLineId,
                ActivityType = GetRecapActivityType(group.OwnerRentSourceTypeId, group.SourceDocumentCode),
                ActivityDate = ParseOwnerAccrualActivityDate(group.TransactionDate),
                AccountingPeriod = accountingPeriod,
                DocumentCode = ResolveOwnerAccrualOwnerRentDocumentCode(group),
                Description = ResolveOwnerAccrualOwnerRentDescription(group),
                ExpectedIncome = group.OwnerRentValue,
                ReceivedIncome = 0,
                Expenses = 0,
                OwnerPayment = 0
            };
        }
        else if (hasOwnerPaymentReceived)
        {
            yield return new OwnerStatementPropertyActivityLine
            {
                PropertyId = group.PropertyId,
                OfficeId = group.OfficeId,
                ActivityId = group.OwnerPaymentJournalEntryLineId,
                SourceId = group.OwnerPaymentSourceId,
                JournalEntryLineId = group.OwnerPaymentJournalEntryLineId,
                ActivityType = GetRecapActivityType(group.OwnerPaymentSourceTypeId, group.SourceDocumentCode),
                ActivityDate = ParseOwnerAccrualActivityDate(group.TransactionDate),
                AccountingPeriod = accountingPeriod,
                DocumentCode = ResolveOwnerAccrualOwnerPaymentDocumentCode(group),
                Description = ResolveOwnerAccrualOwnerPaymentDescription(group),
                ExpectedIncome = 0,
                ReceivedIncome = group.OwnerPaymentReceivedValue,
                Expenses = 0,
                OwnerPayment = 0
            };
        }

        if (group.OwnerExpenseValue != 0)
        {
            yield return new OwnerStatementPropertyActivityLine
            {
                PropertyId = group.PropertyId,
                OfficeId = group.OfficeId,
                ActivityId = group.OwnerExpenseJournalEntryLineId,
                SourceId = group.OwnerExpenseSourceId,
                JournalEntryLineId = group.OwnerExpenseJournalEntryLineId,
                ActivityType = GetRecapActivityType(group.OwnerExpenseSourceTypeId, group.SourceDocumentCode),
                ActivityDate = ParseOwnerAccrualActivityDate(group.TransactionDate),
                AccountingPeriod = accountingPeriod,
                DocumentCode = ResolveOwnerAccrualOwnerExpenseDocumentCode(group),
                Description = ResolveOwnerAccrualOwnerExpenseDescription(group),
                ExpectedIncome = 0,
                ReceivedIncome = 0,
                Expenses = group.OwnerExpenseValue,
                OwnerPayment = 0
            };
        }
    }

    private static string ResolveOwnerAccrualOwnerRentDocumentCode(OwnerAccrualSourceGroup group)
    {
        var ownerRentJournalEntryCode = (group.OwnerRentJournalEntryCode ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(ownerRentJournalEntryCode))
            return ownerRentJournalEntryCode;

        return (group.SourceDocumentCode ?? string.Empty).Trim();
    }

    private static string ResolveOwnerAccrualOwnerRentDescription(OwnerAccrualSourceGroup group)
    {
        var memo = (group.OwnerRentMemo ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(memo))
            return StripOwnerMemoPrefixForCashDisplay(memo);

        return ResolveOwnerAccrualOwnerRentDocumentCode(group);
    }

    private static string ResolveOwnerAccrualOwnerExpenseDocumentCode(OwnerAccrualSourceGroup group)
    {
        var ownerExpenseJournalEntryCode = (group.OwnerExpenseJournalEntryCode ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(ownerExpenseJournalEntryCode))
            return ownerExpenseJournalEntryCode;

        return (group.SourceDocumentCode ?? string.Empty).Trim();
    }

    private static string ResolveOwnerAccrualOwnerExpenseDescription(OwnerAccrualSourceGroup group)
    {
        var memo = (group.OwnerExpenseMemo ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(memo))
            return StripOwnerMemoPrefixForCashDisplay(memo);

        return ResolveOwnerAccrualOwnerExpenseDocumentCode(group);
    }

    private static string ResolveOwnerAccrualOwnerPaymentDocumentCode(OwnerAccrualSourceGroup group)
    {
        var ownerPaymentJournalEntryCode = (group.OwnerPaymentJournalEntryCode ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(ownerPaymentJournalEntryCode))
            return ownerPaymentJournalEntryCode;

        return (group.SourceDocumentCode ?? string.Empty).Trim();
    }

    private static string ResolveOwnerAccrualOwnerPaymentDescription(OwnerAccrualSourceGroup group)
    {
        var memo = (group.OwnerPaymentMemo ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(memo))
            return StripOwnerMemoPrefixForCashDisplay(memo);

        return ResolveOwnerAccrualOwnerPaymentDocumentCode(group);
    }

    private static decimal ResolvePropertyPrepaidIncome(IReadOnlyList<RecapReportRow> propertyRecapRows)
    {
        return propertyRecapRows
            .Where(row => HasOwnerAccrualReportRecapActivity(row))
            .GroupBy(row => row.ReservationCode ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderBy(row => row.AccountingPeriod, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.SortDateValue)
                .Last()
                .PrePaymentValue)
            .Sum();
    }

    private static DateOnly ParseOwnerAccrualActivityDate(string transactionDate)
    {
        if (DateOnly.TryParse(transactionDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        return default;
    }
}

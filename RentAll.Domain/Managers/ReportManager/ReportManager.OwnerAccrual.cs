using System.Globalization;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private sealed class OwnerAccrualInvoiceContext
    {
        public decimal OwnerRentValue { get; set; }
        public decimal ExpectedIncomeValue { get; set; }
    }

    private sealed class OwnerAccrualSourceGroup
    {
        public Guid PropertyId { get; set; }
        public int OfficeId { get; set; }
        public string AccountingPeriod { get; set; } = string.Empty;
        public string OwnerRentAccountingPeriod { get; set; } = string.Empty;
        public string InvoiceSourceCode { get; set; } = string.Empty;
        public string SourceDocumentCode { get; set; } = string.Empty;
        public Guid? SourceId { get; set; }
        public int? SourceTypeId { get; set; }
        public string TransactionDate { get; set; } = string.Empty;
        public long SortDateValue { get; set; }
        public decimal OwnerRentValue { get; set; }
        public decimal ExpectedIncomeValue { get; set; }
        public decimal PaymentValue { get; set; }
        public decimal OwnerPaymentReceivedValue { get; set; }
        public decimal OwnerExpenseValue { get; set; }
        public string OwnerRentMemo { get; set; } = string.Empty;
        public string OwnerExpenseMemo { get; set; } = string.Empty;
        public string OwnerPaymentMemo { get; set; } = string.Empty;
        public string PaymentMemo { get; set; } = string.Empty;
        public string OwnerRentJournalEntryCode { get; set; } = string.Empty;
        public string OwnerExpenseJournalEntryCode { get; set; } = string.Empty;
        public string OwnerPaymentJournalEntryCode { get; set; } = string.Empty;
        public string PaymentJournalEntryCode { get; set; } = string.Empty;
        public Guid? OwnerRentJournalEntryLineId { get; set; }
        public Guid? OwnerExpenseJournalEntryLineId { get; set; }
        public Guid? OwnerPaymentJournalEntryLineId { get; set; }
        public Guid? PaymentJournalEntryLineId { get; set; }
        public Guid? OwnerRentSourceId { get; set; }
        public int? OwnerRentSourceTypeId { get; set; }
        public Guid? OwnerPaymentSourceId { get; set; }
        public int? OwnerPaymentSourceTypeId { get; set; }
        public Guid? PaymentSourceId { get; set; }
        public int? PaymentSourceTypeId { get; set; }
        public Guid? OwnerExpenseSourceId { get; set; }
        public int? OwnerExpenseSourceTypeId { get; set; }
    }

    public async Task<OwnerAccrualReport> GetOwnerAccrualReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        criteria.IncludePaymentInvoiceContext = true;
        var lines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(criteria)).ToList();
        var activitySourceLines = lines.Where(line => line.IsInDateRange).ToList();
        var recapRows = BuildRecapReportRows(activitySourceLines);
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
        var propertyActivityLines = BuildOwnerReportPropertyActivityLines(
            activitySourceLines,
            lines,
            OwnerReportActivityMode.Accrual);
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

    private static Dictionary<string, OwnerAccrualInvoiceContext> BuildOwnerAccrualInvoiceContextByKey(IEnumerable<JournalEntryRecapLine> lines)
    {
        var contextByKey = new Dictionary<string, OwnerAccrualInvoiceContext>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines ?? [])
        {
            if (!TryResolveOwnerAccrualPropertyId(line, out _))
                continue;

            var category = (line.RecapCategory ?? string.Empty).Trim();
            if (!string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(category, "ExpectedIncome", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var contextKey = BuildOwnerAccrualInvoiceContextKey(line);
            if (!contextByKey.TryGetValue(contextKey, out var context))
            {
                context = new OwnerAccrualInvoiceContext();
                contextByKey[contextKey] = context;
            }

            if (string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase))
                context.OwnerRentValue += line.Amount;
            else
                context.ExpectedIncomeValue += line.Amount;
        }

        return contextByKey;
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
        || string.Equals(category, "ExpectedIncome", StringComparison.OrdinalIgnoreCase)
        || string.Equals(category, "Payment", StringComparison.OrdinalIgnoreCase)
        || string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase)
        || string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase);

    private static bool TryResolveOwnerAccrualPropertyId(JournalEntryRecapLine line, out Guid propertyId)
    {
        if (line.PropertyId.HasValue && line.PropertyId.Value != Guid.Empty)
        {
            propertyId = line.PropertyId.Value;
            return true;
        }

        propertyId = Guid.Empty;
        return false;
    }

    private static string BuildOwnerAccrualInvoiceContextKey(JournalEntryRecapLine line)
    {
        if (!TryResolveOwnerAccrualPropertyId(line, out var propertyId))
            propertyId = Guid.Empty;

        return BuildOwnerAccrualInvoiceContextKey(propertyId, ResolveRecapSourceDocumentCode(line), string.Empty);
    }

    private static string BuildOwnerAccrualInvoiceContextKey(Guid propertyId, string sourceDocumentCode, string accountingPeriod)
    {
        var sourceKey = (sourceDocumentCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sourceKey))
            sourceKey = "none";

        return $"{propertyId:D}|{sourceKey}";
    }

    private static string BuildOwnerAccrualSourceGroupKey(JournalEntryRecapLine line, string category)
    {
        if (!TryResolveOwnerAccrualPropertyId(line, out var propertyId))
            propertyId = Guid.Empty;

        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");

        if (string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase))
            return $"{propertyId:D}|{periodKey}|expense|{line.JournalEntryLineId:D}";

        var invoiceSourceKey = ResolveOwnerReportIncomeInvoiceSourceKey(line, category);
        return $"{propertyId:D}|income|{invoiceSourceKey}";
    }

    private static void ApplyOwnerAccrualRecapLine(OwnerAccrualSourceGroup group, JournalEntryRecapLine line, string category)
    {
        var amount = line.Amount;
        var memo = (line.Description ?? string.Empty).Trim();
        var journalEntryCode = (line.JournalEntryCode ?? string.Empty).Trim();

        if (string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase))
        {
            group.OwnerRentValue += amount;
            group.OwnerRentAccountingPeriod = line.AccountingPeriod.ToString("yyyy-MM-dd");
            if (!string.IsNullOrWhiteSpace(memo))
                group.OwnerRentMemo = memo;
            if (!string.IsNullOrWhiteSpace(journalEntryCode))
                group.OwnerRentJournalEntryCode = journalEntryCode;
            group.OwnerRentJournalEntryLineId = line.JournalEntryLineId;
            group.OwnerRentSourceId = line.SourceId;
            group.OwnerRentSourceTypeId = line.SourceTypeId;
            return;
        }

        if (string.Equals(category, "ExpectedIncome", StringComparison.OrdinalIgnoreCase))
        {
            group.ExpectedIncomeValue += amount;
            return;
        }

        if (string.Equals(category, "Payment", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase) && amount < 0))
        {
            group.PaymentValue += Math.Abs(amount);
            if (!string.IsNullOrWhiteSpace(memo))
                group.PaymentMemo = memo;
            if (!string.IsNullOrWhiteSpace(journalEntryCode))
                group.PaymentJournalEntryCode = journalEntryCode;
            group.PaymentJournalEntryLineId = line.JournalEntryLineId;
            group.PaymentSourceId = line.SourceId;
            group.PaymentSourceTypeId = line.SourceTypeId;
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
        || group.OwnerPaymentReceivedValue != 0
        || group.PaymentValue != 0;

    private static bool HasOwnerAccrualReportRecapActivity(RecapReportRow row) =>
        row.OwnerRentValue != 0
        || row.OwnerExpenseValue != 0
        || row.OwnerPaymentReceivedValue != 0
        || row.PaymentValue != 0;

    private static decimal ResolveOwnerAccrualPaidIncomeForGroup(
        OwnerAccrualSourceGroup group,
        string invoiceContextKey,
        IReadOnlyDictionary<string, OwnerAccrualInvoiceContext> invoiceContextByKey)
    {
        if (group.OwnerPaymentReceivedValue != 0)
            return group.OwnerPaymentReceivedValue;

        if (group.PaymentValue == 0)
            return 0;

        invoiceContextByKey.TryGetValue(invoiceContextKey, out var invoiceContext);
        var ownerRent = group.OwnerRentValue != 0
            ? group.OwnerRentValue
            : invoiceContext?.OwnerRentValue ?? 0;
        var expectedIncome = group.ExpectedIncomeValue != 0
            ? group.ExpectedIncomeValue
            : invoiceContext?.ExpectedIncomeValue ?? 0;

        if (ownerRent == 0 || expectedIncome == 0)
            return 0;

        var ownerPaid = group.PaymentValue * ownerRent / expectedIncome;
        if (ownerPaid < 0)
            return 0;

        return ownerPaid > ownerRent ? ownerRent : ownerPaid;
    }

    private static IEnumerable<OwnerStatementPropertyActivityLine> ExpandOwnerAccrualSourceGroupActivityLines(
        OwnerAccrualSourceGroup group,
        string invoiceContextKey,
        IReadOnlyDictionary<string, OwnerAccrualInvoiceContext> invoiceContextByKey,
        OwnerReportActivityMode mode)
    {
        var paidIncome = ResolveOwnerAccrualPaidIncomeForGroup(group, invoiceContextKey, invoiceContextByKey);
        var hasOwnerRent = group.OwnerRentValue != 0;
        var hasPaidIncome = paidIncome != 0;
        var isAccrual = mode == OwnerReportActivityMode.Accrual;
        var ownerRentAccountingPeriod = FormatJournalEntryRecapAccountingPeriod(
            group.OwnerRentAccountingPeriod ?? group.AccountingPeriod);
        var paymentAccountingPeriod = FormatJournalEntryRecapAccountingPeriod(group.AccountingPeriod);

        if (hasOwnerRent && hasPaidIncome)
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
                AccountingPeriod = ownerRentAccountingPeriod,
                DocumentCode = ResolveOwnerAccrualOwnerRentDocumentCode(group),
                Description = ResolveOwnerAccrualOwnerRentDescription(group),
                ExpectedIncome = isAccrual ? group.OwnerRentValue : 0,
                ReceivedIncome = paidIncome,
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
                AccountingPeriod = ownerRentAccountingPeriod,
                DocumentCode = ResolveOwnerAccrualOwnerRentDocumentCode(group),
                Description = ResolveOwnerAccrualOwnerRentDescription(group),
                ExpectedIncome = isAccrual ? group.OwnerRentValue : 0,
                ReceivedIncome = 0,
                Expenses = 0,
                OwnerPayment = 0
            };
        }
        else if (hasPaidIncome)
        {
            yield return new OwnerStatementPropertyActivityLine
            {
                PropertyId = group.PropertyId,
                OfficeId = group.OfficeId,
                ActivityId = group.PaymentJournalEntryLineId ?? group.OwnerPaymentJournalEntryLineId,
                SourceId = group.PaymentSourceId ?? group.OwnerPaymentSourceId,
                JournalEntryLineId = group.PaymentJournalEntryLineId ?? group.OwnerPaymentJournalEntryLineId,
                ActivityType = GetRecapActivityType(
                    group.PaymentSourceTypeId ?? group.OwnerPaymentSourceTypeId,
                    group.SourceDocumentCode),
                ActivityDate = ParseOwnerAccrualActivityDate(group.TransactionDate),
                AccountingPeriod = paymentAccountingPeriod,
                DocumentCode = ResolveOwnerAccrualTenantPaymentDocumentCode(group),
                Description = ResolveOwnerAccrualTenantPaymentDescription(group),
                ExpectedIncome = 0,
                ReceivedIncome = paidIncome,
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
                AccountingPeriod = FormatJournalEntryRecapAccountingPeriod(group.AccountingPeriod),
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

    private static string ResolveOwnerAccrualTenantPaymentDocumentCode(OwnerAccrualSourceGroup group)
    {
        var paymentJournalEntryCode = (group.PaymentJournalEntryCode ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(paymentJournalEntryCode))
            return paymentJournalEntryCode;

        var ownerPaymentJournalEntryCode = (group.OwnerPaymentJournalEntryCode ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(ownerPaymentJournalEntryCode))
            return ownerPaymentJournalEntryCode;

        return (group.SourceDocumentCode ?? string.Empty).Trim();
    }

    private static string ResolveOwnerAccrualTenantPaymentDescription(OwnerAccrualSourceGroup group)
    {
        var memo = (group.PaymentMemo ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(memo))
            return StripTenantPaymentMemoForDisplay(memo);

        memo = (group.OwnerPaymentMemo ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(memo))
            return StripOwnerMemoPrefixForCashDisplay(memo);

        return ResolveOwnerAccrualTenantPaymentDocumentCode(group);
    }

    private static string StripTenantPaymentMemoForDisplay(string memo)
    {
        var trimmed = (memo ?? string.Empty).Trim();
        if (trimmed.StartsWith("Payment:", StringComparison.OrdinalIgnoreCase))
            return trimmed["Payment:".Length..].TrimStart();

        if (trimmed.StartsWith("Prepayment:", StringComparison.OrdinalIgnoreCase))
            return trimmed["Prepayment:".Length..].TrimStart();

        return trimmed;
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

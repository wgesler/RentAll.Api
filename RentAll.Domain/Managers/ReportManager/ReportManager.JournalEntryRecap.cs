using System.Globalization;
using System.Text.RegularExpressions;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private sealed class GroupAccumulator
    {
        public string PropertyCode { get; set; } = string.Empty;
        public string ReservationCode { get; set; } = string.Empty;
        public string ReservationKey { get; set; } = string.Empty;
        public string PropertyKey { get; set; } = string.Empty;
        public string PropertyId { get; set; } = string.Empty;
        public string ReservationId { get; set; } = string.Empty;
        public int OfficeId { get; set; }
        public string AccountingPeriod { get; set; } = string.Empty;
        public int? SourceTypeId { get; set; }
        public Guid? SourceId { get; set; }
        public string SourceDocumentCode { get; set; } = string.Empty;
        public string JournalEntryCode { get; set; } = string.Empty;
        public string Memo { get; set; } = string.Empty;
        public string OwnerRentMemo { get; set; } = string.Empty;
        public string OwnerExpenseMemo { get; set; } = string.Empty;
        public string OwnerPaymentMemo { get; set; } = string.Empty;
        public string OwnerRentJournalEntryCode { get; set; } = string.Empty;
        public string OwnerExpenseJournalEntryCode { get; set; } = string.Empty;
        public string OwnerPaymentJournalEntryCode { get; set; } = string.Empty;
        public Guid? OwnerRentJournalEntryId { get; set; }
        public Guid? OwnerRentJournalEntryLineId { get; set; }
        public Guid? OwnerExpenseJournalEntryLineId { get; set; }
        public Guid? OwnerPaymentJournalEntryLineId { get; set; }
        public int SourcePriority { get; set; } = -1;
        public int JournalEntryPriority { get; set; } = -1;
        public string TransactionDate { get; set; } = string.Empty;
        public long SortDateValue { get; set; }
        public Guid? JournalEntryId { get; set; }
        public Guid? JournalEntryLineId { get; set; }
        public bool IsPosted { get; set; }
        public decimal ExpectedIncomeValue { get; set; }
        public decimal RentPlus4000Value { get; set; }
        public decimal SecurityDepositValue { get; set; }
        public decimal SdwValue { get; set; }
        public decimal FeeValue { get; set; }
        public decimal PaymentValue { get; set; }
        public decimal PrePaymentValue { get; set; }
        public decimal OwnerRentValue { get; set; }
        public decimal OwnerExpenseValue { get; set; }
        public decimal OwnerPaymentValue { get; set; }
    }

    private static readonly Dictionary<string, int> SourcePriorityByCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ExpectedIncome"] = 100,
        ["PrePayment"] = 90,
        ["OwnerRent"] = 80,
        ["Payment"] = 60,
        ["RentPlus4000"] = 55,
        ["Expense"] = 50
    };

    private static readonly Regex RecapSourceCodePattern = new(
        @"\b(?:WO-[A-Za-z0-9-]+|R-\d+(?:-\d+)*|RC[A-Za-z0-9-]*)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RecapMemoSourceCodePattern = new(
        @"(?:Payment|Prepayment|Invoice)\s*:\s*([A-Za-z0-9-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ReservationCodePattern = new(
        @"^R-\d+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<RecapReport> GetJournalEntryRecapReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        var lines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(criteria)).ToList();
        return new RecapReport
        {
            Rows = BuildRecapReportRows(lines)
        };
    }

    private static List<RecapReportRow> BuildRecapReportRows(IEnumerable<JournalEntryRecapLine> lines)
    {
        var groups = new Dictionary<string, GroupAccumulator>(StringComparer.OrdinalIgnoreCase);
        var propertyLevelExpenseGroups = new List<GroupAccumulator>();
        var prePayAppliedByPeriod = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var prePayReceivedByPeriod = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines ?? [])
        {
            var category = (line.RecapCategory ?? string.Empty).Trim();
            var amount = line.Amount;
            var reservationKey = BuildReservationKey(line);
            var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");

            if (string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase))
            {
                if (amount > 0 && !string.IsNullOrWhiteSpace(reservationKey) && !string.IsNullOrWhiteSpace(periodKey))
                {
                    var applyKey = $"{reservationKey}|{periodKey}";
                    prePayReceivedByPeriod[applyKey] = prePayReceivedByPeriod.GetValueOrDefault(applyKey) + amount;
                    var group = GetOrCreateGroup(groups, line);
                    SetPrimaryJournalEntry(group, line, category);
                    CaptureCategoryActivityContext(group, line, category);
                    EnrichGroupSource(group, line, category);
                    TouchGroupMetadata(group, line);
                }
                else if (amount < 0 && !string.IsNullOrWhiteSpace(reservationKey) && !string.IsNullOrWhiteSpace(periodKey))
                {
                    var applyKey = $"{reservationKey}|{periodKey}";
                    prePayAppliedByPeriod[applyKey] = prePayAppliedByPeriod.GetValueOrDefault(applyKey) + Math.Abs(amount);
                }

                continue;
            }

            if (string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(reservationKey))
            {
                propertyLevelExpenseGroups.Add(CreatePropertyLevelExpenseGroup(line, amount));
                continue;
            }

            var recapGroup = GetOrCreateGroup(groups, line);
            ApplyCategoryAmount(recapGroup, category, amount);
            SetPrimaryJournalEntry(recapGroup, line, category);
            CaptureCategoryActivityContext(recapGroup, line, category);
            EnrichGroupSource(recapGroup, line, category);
            TouchGroupMetadata(recapGroup, line);
        }

        var reservationPeriodKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups.Values)
        {
            if (!string.IsNullOrWhiteSpace(group.ReservationKey) && !string.IsNullOrWhiteSpace(group.AccountingPeriod))
                reservationPeriodKeys.Add($"{group.ReservationKey}|{group.AccountingPeriod}");
        }

        foreach (var key in prePayReceivedByPeriod.Keys)
            reservationPeriodKeys.Add(key);
        foreach (var key in prePayAppliedByPeriod.Keys)
            reservationPeriodKeys.Add(key);

        var reservationKeys = reservationPeriodKeys
            .Select(key => key.Split('|')[0])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var reservationKey in reservationKeys)
        {
            var sortedPeriods = reservationPeriodKeys
                .Where(key => key.StartsWith($"{reservationKey}|", StringComparison.OrdinalIgnoreCase))
                .Select(key => key[(reservationKey.Length + 1)..])
                .OrderBy(period => period, StringComparer.Ordinal)
                .ToList();

            var runningPrePayBalance = 0m;
            foreach (var period in sortedPeriods)
            {
                var group = groups.Values.FirstOrDefault(item =>
                    string.Equals(item.ReservationKey, reservationKey, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(item.AccountingPeriod, period, StringComparison.OrdinalIgnoreCase));
                if (group == null)
                    continue;

                var receivedAmount = prePayReceivedByPeriod.GetValueOrDefault($"{reservationKey}|{period}");
                var appliedAmount = prePayAppliedByPeriod.GetValueOrDefault($"{reservationKey}|{period}");
                runningPrePayBalance += receivedAmount;

                if (appliedAmount > 0)
                {
                    group.PaymentValue = appliedAmount;
                    runningPrePayBalance -= appliedAmount;
                    if (runningPrePayBalance < 0)
                        runningPrePayBalance = 0;
                    group.PrePaymentValue = runningPrePayBalance;
                }
                else if (receivedAmount > 0)
                {
                    group.PaymentValue = receivedAmount;
                    group.PrePaymentValue = runningPrePayBalance;
                }
            }
        }

        foreach (var group in groups.Values)
        {
            if (!string.IsNullOrWhiteSpace(group.SourceDocumentCode))
                continue;

            var reservationCode = (group.ReservationCode ?? string.Empty).Trim();
            if (ReservationCodePattern.IsMatch(reservationCode)
                && (group.ExpectedIncomeValue != 0 || group.PaymentValue != 0 || group.PrePaymentValue != 0))
            {
                group.SourceDocumentCode = reservationCode;
            }
        }

        return groups.Values
            .Concat(propertyLevelExpenseGroups)
            .Where(HasMeaningfulAmount)
            .OrderBy(group => group.PropertyCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.ReservationCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.AccountingPeriod, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.SortDateValue)
            .ThenBy(group => group.JournalEntryLineId)
            .Select(MapRecapReportRow)
            .ToList();
    }

    private static decimal CalculateRecapOwnerPaymentValue(GroupAccumulator group)
    {
        // OwnPay = Owner Rent - Owner Expenses; never display or return a negative value.
        var ownerPayment = group.OwnerRentValue - group.OwnerExpenseValue;
        return ownerPayment < 0 ? 0 : ownerPayment;
    }

    private static RecapReportRow MapRecapReportRow(GroupAccumulator group)
    {
        var ownerPaymentValue = CalculateRecapOwnerPaymentValue(group);
        var sourceDocumentCode = (group.SourceDocumentCode ?? string.Empty).Trim();
        return new RecapReportRow
        {
            PropertyCode = group.PropertyCode,
            ReservationCode = group.ReservationCode,
            AccountingPeriod = FormatJournalEntryRecapAccountingPeriod(group.AccountingPeriod),
            Source = sourceDocumentCode,
            JournalEntryCode = group.JournalEntryCode,
            Memo = group.Memo,
            OwnerRentMemo = group.OwnerRentMemo,
            OwnerExpenseMemo = group.OwnerExpenseMemo,
            OwnerPaymentMemo = group.OwnerPaymentMemo,
            OwnerRentJournalEntryCode = group.OwnerRentJournalEntryCode,
            OwnerExpenseJournalEntryCode = group.OwnerExpenseJournalEntryCode,
            OwnerPaymentJournalEntryCode = group.OwnerPaymentJournalEntryCode,
            OwnerRentJournalEntryId = group.OwnerRentJournalEntryId,
            OwnerRentJournalEntryLineId = group.OwnerRentJournalEntryLineId,
            OwnerExpenseJournalEntryLineId = group.OwnerExpenseJournalEntryLineId,
            OwnerPaymentJournalEntryLineId = group.OwnerPaymentJournalEntryLineId,
            SourceTypeId = group.SourceTypeId,
            SourceId = group.SourceId,
            SourceLinkable = IsRecapSourceLinkable(group.SourceTypeId, group.SourceId, sourceDocumentCode),
            ActivityType = GetRecapActivityType(group.SourceTypeId, sourceDocumentCode),
            OfficeId = group.OfficeId,
            PropertyId = ParseGuidOrNull(group.PropertyId),
            ReservationId = ParseGuidOrNull(group.ReservationId),
            TransactionDate = FormatDateString(group.TransactionDate),
            ExpectedIncome = FormatCurrencyUsd(group.ExpectedIncomeValue),
            RentPlus4000 = FormatCurrencyUsd(group.RentPlus4000Value),
            SecurityDeposit = FormatCurrencyUsd(group.SecurityDepositValue),
            Sdw = FormatCurrencyUsd(group.SdwValue),
            Fee = FormatCurrencyUsd(group.FeeValue),
            Payment = FormatCurrencyUsd(group.PaymentValue),
            PrePayment = FormatCurrencyUsd(group.PrePaymentValue),
            OwnerRent = FormatCurrencyUsd(group.OwnerRentValue),
            OwnerExpense = FormatCurrencyUsd(group.OwnerExpenseValue),
            OwnerPayment = FormatCurrencyUsd(ownerPaymentValue),
            ExpectedIncomeValue = group.ExpectedIncomeValue,
            RentPlus4000Value = group.RentPlus4000Value,
            SecurityDepositValue = group.SecurityDepositValue,
            SdwValue = group.SdwValue,
            FeeValue = group.FeeValue,
            PaymentValue = group.PaymentValue,
            PrePaymentValue = group.PrePaymentValue,
            OwnerRentValue = group.OwnerRentValue,
            OwnerExpenseValue = group.OwnerExpenseValue,
            OwnerPaymentValue = ownerPaymentValue,
            SortDateValue = group.SortDateValue,
            JournalEntryId = group.JournalEntryId,
            JournalEntryLineId = group.JournalEntryLineId,
            IsPosted = group.IsPosted
        };
    }

    private static GroupAccumulator GetOrCreateGroup(
        Dictionary<string, GroupAccumulator> groups,
        JournalEntryRecapLine line)
    {
        var propertyKey = BuildPropertyKey(line);
        var reservationKey = BuildReservationKey(line);
        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
        var groupKey = BuildGroupKey(propertyKey, reservationKey, periodKey);
        if (groups.TryGetValue(groupKey, out var group))
            return group;

        group = new GroupAccumulator
        {
            PropertyCode = (line.PropertyCode ?? string.Empty).Trim(),
            ReservationCode = (line.ReservationCode ?? string.Empty).Trim(),
            ReservationKey = reservationKey,
            PropertyKey = propertyKey,
            PropertyId = (line.PropertyId?.ToString() ?? string.Empty).Trim(),
            ReservationId = (line.ReservationId?.ToString() ?? string.Empty).Trim(),
            OfficeId = line.OfficeId,
            AccountingPeriod = periodKey,
            TransactionDate = line.TransactionDate.ToString("yyyy-MM-dd"),
            SortDateValue = line.TransactionDate.ToDateTime(TimeOnly.MinValue).Ticks
        };
        groups[groupKey] = group;
        return group;
    }

    private static void ApplyCategoryAmount(GroupAccumulator group, string category, decimal amount)
    {
        switch (category)
        {
            case "ExpectedIncome":
                group.ExpectedIncomeValue += amount;
                break;
            case "RentPlus4000":
                group.RentPlus4000Value += amount;
                break;
            case "SecurityDeposit":
                group.SecurityDepositValue += amount;
                break;
            case "SDW":
                group.SdwValue += amount;
                break;
            case "Fee":
                group.FeeValue += amount;
                break;
            case "Payment":
                group.PaymentValue += amount;
                break;
            case "OwnerRent":
                group.OwnerRentValue += amount;
                break;
            case "Expense":
                group.OwnerExpenseValue += amount;
                break;
        }
    }

    private static void SetPrimaryJournalEntry(GroupAccumulator group, JournalEntryRecapLine line, string category)
    {
        var priority = SourcePriorityByCategory.GetValueOrDefault(category);
        var journalEntryCode = (line.JournalEntryCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(journalEntryCode) || priority < group.JournalEntryPriority)
            return;

        group.JournalEntryPriority = priority;
        group.JournalEntryCode = journalEntryCode;
        group.Memo = (line.Description ?? string.Empty).Trim();
        group.JournalEntryId = line.JournalEntryId;
        group.JournalEntryLineId = line.JournalEntryLineId;
        group.IsPosted = line.IsPosted;
        group.OfficeId = line.OfficeId;
        if (line.PropertyId.HasValue)
            group.PropertyId = line.PropertyId.Value.ToString();
        if (line.ReservationId.HasValue)
            group.ReservationId = line.ReservationId.Value.ToString();
    }

    private static void CaptureCategoryActivityContext(GroupAccumulator group, JournalEntryRecapLine line, string category)
    {
        var memo = (line.Description ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(memo))
            return;

        var journalEntryCode = (line.JournalEntryCode ?? string.Empty).Trim();

        if (string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase))
        {
            group.OwnerRentMemo = memo;
            if (!string.IsNullOrWhiteSpace(journalEntryCode))
                group.OwnerRentJournalEntryCode = journalEntryCode;
            group.OwnerRentJournalEntryId = line.JournalEntryId;
            group.OwnerRentJournalEntryLineId = line.JournalEntryLineId;
            return;
        }

        if (string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase))
        {
            group.OwnerExpenseMemo = memo;
            if (!string.IsNullOrWhiteSpace(journalEntryCode))
                group.OwnerExpenseJournalEntryCode = journalEntryCode;
            group.OwnerExpenseJournalEntryLineId = line.JournalEntryLineId;
            return;
        }

        if (string.Equals(category, "OwnerPayment", StringComparison.OrdinalIgnoreCase))
        {
            group.OwnerPaymentMemo = memo;
            if (!string.IsNullOrWhiteSpace(journalEntryCode))
                group.OwnerPaymentJournalEntryCode = journalEntryCode;
            group.OwnerPaymentJournalEntryLineId = line.JournalEntryLineId;
        }
    }

    private static void EnrichGroupSource(GroupAccumulator group, JournalEntryRecapLine line, string category)
    {
        var sourceDocumentCode = ResolveRecapSourceDocumentCode(line);
        if (string.IsNullOrWhiteSpace(sourceDocumentCode))
            return;

        var priority = SourcePriorityByCategory.GetValueOrDefault(category);
        if (!string.IsNullOrWhiteSpace(group.SourceDocumentCode) && priority < group.SourcePriority)
            return;

        group.SourcePriority = priority;
        group.SourceDocumentCode = sourceDocumentCode;
        group.SourceTypeId = line.SourceTypeId ?? group.SourceTypeId;
        group.SourceId = line.SourceId ?? group.SourceId;
        group.OfficeId = line.OfficeId;
        if (line.PropertyId.HasValue)
            group.PropertyId = line.PropertyId.Value.ToString();
        if (line.ReservationId.HasValue)
            group.ReservationId = line.ReservationId.Value.ToString();
    }

    private static void TouchGroupMetadata(GroupAccumulator group, JournalEntryRecapLine line)
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
    }

    private static bool HasMeaningfulAmount(GroupAccumulator group) =>
        group.ExpectedIncomeValue != 0
        || group.RentPlus4000Value != 0
        || group.SecurityDepositValue != 0
        || group.SdwValue != 0
        || group.FeeValue != 0
        || group.PaymentValue != 0
        || group.PrePaymentValue != 0
        || group.OwnerRentValue != 0
        || group.OwnerExpenseValue != 0
        || CalculateRecapOwnerPaymentValue(group) != 0;

    private static string BuildPropertyKey(JournalEntryRecapLine line) =>
        (line.PropertyCode ?? line.PropertyId?.ToString() ?? string.Empty).Trim();

    private static string BuildReservationKey(JournalEntryRecapLine line) =>
        (line.ReservationCode ?? line.ReservationId?.ToString() ?? string.Empty).Trim();

    private static GroupAccumulator CreatePropertyLevelExpenseGroup(JournalEntryRecapLine line, decimal amount)
    {
        var reservationKey = BuildReservationKey(line);
        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
        var group = new GroupAccumulator
        {
            PropertyCode = (line.PropertyCode ?? string.Empty).Trim(),
            ReservationCode = (line.ReservationCode ?? string.Empty).Trim(),
            ReservationKey = reservationKey,
            PropertyKey = BuildPropertyKey(line),
            PropertyId = (line.PropertyId?.ToString() ?? string.Empty).Trim(),
            ReservationId = (line.ReservationId?.ToString() ?? string.Empty).Trim(),
            OfficeId = line.OfficeId,
            AccountingPeriod = periodKey,
            TransactionDate = line.TransactionDate.ToString("yyyy-MM-dd"),
            SortDateValue = line.TransactionDate.ToDateTime(TimeOnly.MinValue).Ticks,
            JournalEntryId = line.JournalEntryId,
            JournalEntryLineId = line.JournalEntryLineId
        };

        const string category = "Expense";
        ApplyCategoryAmount(group, category, amount);
        SetPrimaryJournalEntry(group, line, category);
        CaptureCategoryActivityContext(group, line, category);
        EnrichGroupSource(group, line, category);
        TouchGroupMetadata(group, line);
        return group;
    }

    private static string BuildGroupKey(string propertyKey, string reservationKey, string periodKey) =>
        $"{propertyKey}|{reservationKey}|{periodKey}";

    private static string ResolveRecapSourceDocumentCode(JournalEntryRecapLine line)
    {
        var fromProc = (line.SourceDocumentCode ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(fromProc))
            return fromProc;

        var description = (line.Description ?? string.Empty).Trim();
        var memoMatch = RecapMemoSourceCodePattern.Match(description);
        if (memoMatch.Success && memoMatch.Groups.Count > 1)
            return memoMatch.Groups[1].Value.Trim();

        var codeMatch = RecapSourceCodePattern.Match(description);
        if (codeMatch.Success)
            return codeMatch.Value.Trim();

        if (line.SourceTypeId is (int)SourceType.Invoice or (int)SourceType.InvoicePayment
            || string.Equals(line.RecapCategory, "ExpectedIncome", StringComparison.OrdinalIgnoreCase)
            || string.Equals(line.RecapCategory, "Payment", StringComparison.OrdinalIgnoreCase)
            || string.Equals(line.RecapCategory, "PrePayment", StringComparison.OrdinalIgnoreCase))
        {
            var reservationCode = (line.ReservationCode ?? string.Empty).Trim();
            if (ReservationCodePattern.IsMatch(reservationCode))
                return reservationCode;
        }

        return string.Empty;
    }

    private static string GetRecapActivityType(int? sourceTypeId, string documentCode)
    {
        switch (sourceTypeId)
        {
            case (int)SourceType.WorkOrder:
                return "workorder";
            case (int)SourceType.Bill:
            case (int)SourceType.BillPayment:
                return "bill";
            case (int)SourceType.Receipt:
                return "receipt";
            case (int)SourceType.LinensAndTowels:
                return "linensandtowels";
            case (int)SourceType.Invoice:
            case (int)SourceType.InvoicePayment:
                return "invoice";
        }

        var normalizedCode = (documentCode ?? string.Empty).Trim();
        if (normalizedCode.StartsWith("WO-", StringComparison.OrdinalIgnoreCase))
            return "workorder";
        if (normalizedCode.StartsWith("RC", StringComparison.OrdinalIgnoreCase))
            return "receipt";
        if (ReservationCodePattern.IsMatch(normalizedCode))
            return "reservation";

        return string.Empty;
    }

    private static bool IsRecapSourceLinkable(int? sourceTypeId, Guid? sourceId, string documentCode)
    {
        var normalizedCode = (documentCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
            return false;

        if (RecapSourceCodePattern.IsMatch(normalizedCode))
            return true;

        if (sourceTypeId == (int)SourceType.WorkOrder)
            return true;

        return IsJournalEntrySourceNavigable(sourceTypeId) && sourceId.HasValue && sourceId.Value != Guid.Empty;
    }

    private static bool IsJournalEntrySourceNavigable(int? sourceTypeId)
    {
        if (!sourceTypeId.HasValue)
            return false;

        return sourceTypeId.Value is (int)SourceType.Invoice
            or (int)SourceType.InvoicePayment
            or (int)SourceType.Bill
            or (int)SourceType.BillPayment
            or (int)SourceType.Receipt;
    }

    private static string FormatJournalEntryRecapAccountingPeriod(string accountingPeriod)
    {
        if (string.IsNullOrWhiteSpace(accountingPeriod))
            return "—";

        if (!DateOnly.TryParse(accountingPeriod, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return "—";

        return parsed.ToString("MM.yy", CultureInfo.InvariantCulture);
    }

    private static string FormatDateString(string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return string.Empty;

        if (!DateOnly.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return string.Empty;

        return parsed.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
    }

    private static string FormatCurrencyUsd(decimal value)
    {
        var absolute = Math.Abs(value);
        var formatted = absolute.ToString("N2", CultureInfo.InvariantCulture);
        return value < 0 ? $"-${formatted}" : $"${formatted}";
    }

    private static Guid? ParseGuidOrNull(string value)
    {
        return Guid.TryParse(value, out var parsed) && parsed != Guid.Empty ? parsed : null;
    }
}

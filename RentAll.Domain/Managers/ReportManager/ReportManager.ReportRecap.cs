using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{

    public async Task<RecapReport> GetJournalEntryRecapReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        var lines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(criteria)).ToList();
        return new RecapReport
        {
            Rows = BuildRecapReportRows(lines)
        };
    }

    #region Build

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
            var reservationKey = GetReservationKey(line);
            var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");

            if (string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase))
            {
                if (amount > 0 && !string.IsNullOrWhiteSpace(reservationKey) && !string.IsNullOrWhiteSpace(periodKey))
                {
                    var applyKey = $"{reservationKey}|{periodKey}";
                    prePayReceivedByPeriod[applyKey] = prePayReceivedByPeriod.GetValueOrDefault(applyKey) + amount;
                    var group = GetOrCreateGroup(groups, line);
                    RollupRecapPrimaryJournalEntry(group, line, category);
                    RollupRecapCategoryJournalEntryDetails(group, line, category);
                    RollupRecapSourceDocument(group, line, category);
                    RollupRecapEarliestTransactionDate(group, line);
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
                propertyLevelExpenseGroups.Add(BuildRecapPropertyLevelExpenseGroup(line, amount));
                continue;
            }

            var recapGroup = GetOrCreateGroup(groups, line);
            RollupRecapCategoryAmount(recapGroup, category, amount);
            RollupRecapPrimaryJournalEntry(recapGroup, line, category);
            RollupRecapCategoryJournalEntryDetails(recapGroup, line, category);
            RollupRecapSourceDocument(recapGroup, line, category);
            RollupRecapEarliestTransactionDate(recapGroup, line);
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
            .ThenBy(group => group.AccountingPeriod, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.ReservationCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.SortDateValue)
            .ThenBy(group => group.JournalEntryLineId)
            .Select(BuildRecapReportRow)
            .ToList();
    }

    private static RecapReportRow BuildRecapReportRow(GroupAccumulator group)
    {
        var ownerPaidRentValue = CalculateRecapPaidOwnerRent(group);
        var ownerPaymentValue = CalculateRecapOwnerPayment(group);
        var unPaidValue = CalculateRecapUnPaidIncome(group);
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
            UnPaid = FormatCurrencyUsd(unPaidValue),
            OwnerRent = FormatCurrencyUsd(ownerPaidRentValue),
            OwnerExpense = FormatCurrencyUsd(group.OwnerExpenseValue),
            OwnerPayment = FormatCurrencyUsd(ownerPaymentValue),
            ExpectedIncomeValue = group.ExpectedIncomeValue,
            RentPlus4000Value = group.RentPlus4000Value,
            SecurityDepositValue = group.SecurityDepositValue,
            SdwValue = group.SdwValue,
            FeeValue = group.FeeValue,
            PaymentValue = group.PaymentValue,
            PrePaymentValue = group.PrePaymentValue,
            UnPaidValue = unPaidValue,
            OwnerRentValue = ownerPaidRentValue,
            OwnerExpenseValue = group.OwnerExpenseValue,
            OwnerPaymentReceivedValue = group.OwnerPaymentReceivedValue,
            OwnerPaymentValue = ownerPaymentValue,
            PaymentMemo = group.PaymentMemo,
            PaymentJournalEntryCode = group.PaymentJournalEntryCode,
            PaymentJournalEntryLineId = group.PaymentJournalEntryLineId,
            PaymentTransactionDate = FormatDateString(group.PaymentTransactionDate),
            PaymentSortDateValue = group.PaymentSortDateValue,
            SortDateValue = group.SortDateValue,
            JournalEntryId = group.JournalEntryId,
            JournalEntryLineId = group.JournalEntryLineId,
            IsPosted = group.IsPosted
        };
    }

    private static GroupAccumulator BuildRecapPropertyLevelExpenseGroup(JournalEntryRecapLine line, decimal amount)
    {
        var reservationKey = GetReservationKey(line);
        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
        var group = new GroupAccumulator
        {
            PropertyCode = (line.PropertyCode ?? string.Empty).Trim(),
            ReservationCode = (line.ReservationCode ?? string.Empty).Trim(),
            ReservationKey = reservationKey,
            PropertyKey = GetPropertyKey(line),
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
        RollupRecapCategoryAmount(group, category, amount);
        RollupRecapPrimaryJournalEntry(group, line, category);
        RollupRecapCategoryJournalEntryDetails(group, line, category);
        RollupRecapSourceDocument(group, line, category);
        RollupRecapEarliestTransactionDate(group, line);
        return group;
    }

    #endregion

    #region Rollup

    private static void RollupRecapCategoryAmount(GroupAccumulator group, string category, decimal amount)
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
            case "OwnerPayment":
                group.OwnerPaymentReceivedValue += amount;
                break;
            case "Expense":
                group.OwnerExpenseValue += amount;
                break;
        }
    }

    private static void RollupRecapPrimaryJournalEntry(GroupAccumulator group, JournalEntryRecapLine line, string category)
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

    private static void RollupRecapCategoryJournalEntryDetails(GroupAccumulator group, JournalEntryRecapLine line, string category)
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
            return;
        }

        if (string.Equals(category, "Payment", StringComparison.OrdinalIgnoreCase))
        {
            group.PaymentMemo = memo;
            if (!string.IsNullOrWhiteSpace(journalEntryCode))
                group.PaymentJournalEntryCode = journalEntryCode;
            group.PaymentJournalEntryLineId = line.JournalEntryLineId;
            var paymentTransactionDate = line.TransactionDate.ToString("yyyy-MM-dd");
            if (!string.IsNullOrWhiteSpace(paymentTransactionDate))
            {
                group.PaymentTransactionDate = paymentTransactionDate;
                group.PaymentSortDateValue = line.TransactionDate.ToDateTime(TimeOnly.MinValue).Ticks;
            }
        }
    }

    private static void RollupRecapSourceDocument(GroupAccumulator group, JournalEntryRecapLine line, string category)
    {
        var sourceDocumentCode = GetRecapSourceDocumentCode(line);
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

    private static void RollupRecapEarliestTransactionDate(GroupAccumulator group, JournalEntryRecapLine line)
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

    #endregion

    #region Calculate

    private static decimal CalculateRecapPaidOwnerRent(GroupAccumulator group) => CalculateOwnerPaidIncome(group.OwnerRentValue, group.ExpectedIncomeValue, group.PaymentValue, group.OwnerPaymentReceivedValue);

    private static decimal CalculateRecapOwnerPayment(GroupAccumulator group)
    {
        // OwnPay = collected owner rent - owner expenses; never display or return a negative value.
        var ownerPayment = CalculateRecapPaidOwnerRent(group) - group.OwnerExpenseValue;
        return ownerPayment < 0 ? 0 : ownerPayment;
    }

    private static decimal CalculateRecapUnPaidIncome(GroupAccumulator group) => CalculateUnpaidIncome(group.OwnerRentValue, CalculateRecapPaidOwnerRent(group));

    #endregion

    #region Get

    private static GroupAccumulator GetOrCreateGroup(Dictionary<string, GroupAccumulator> groups, JournalEntryRecapLine line)
    {
        var propertyKey = GetPropertyKey(line);
        var reservationKey = GetReservationKey(line);
        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
        var groupKey = GetGroupKey(propertyKey, reservationKey, periodKey);
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

    private static string GetPropertyKey(JournalEntryRecapLine line) =>
        (line.PropertyCode ?? line.PropertyId?.ToString() ?? string.Empty).Trim();

    private static string GetReservationKey(JournalEntryRecapLine line) =>
        (line.ReservationCode ?? line.ReservationId?.ToString() ?? string.Empty).Trim();

    private static string GetGroupKey(string propertyKey, string reservationKey, string periodKey) =>
        $"{propertyKey}|{reservationKey}|{periodKey}";

    private static string GetRecapSourceDocumentCode(JournalEntryRecapLine line)
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

    private static IEnumerable<string> GetRecapPaymentInvoiceSourceCodes(JournalEntryRecapLine line)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var description = (line.Description ?? string.Empty).Trim();

        var memoMatch = RecapMemoSourceCodePattern.Match(description);
        if (memoMatch.Success && memoMatch.Groups.Count > 1)
        {
            var memoCode = memoMatch.Groups[1].Value.Trim();
            if (IsRecapInvoiceSourceDocumentCode(memoCode))
                codes.Add(memoCode);
        }

        foreach (Match match in RecapSourceCodePattern.Matches(description))
        {
            var code = match.Value.Trim();
            if (IsRecapInvoiceSourceDocumentCode(code))
                codes.Add(code);
        }

        var sourceDocumentCode = GetRecapSourceDocumentCode(line);
        if (IsRecapInvoiceSourceDocumentCode(sourceDocumentCode))
            codes.Add(sourceDocumentCode);

        return codes;
    }

    #endregion

    #region Helpers

    private static bool HasMeaningfulAmount(GroupAccumulator group) =>
        group.ExpectedIncomeValue != 0
        || group.RentPlus4000Value != 0
        || group.SecurityDepositValue != 0
        || group.SdwValue != 0
        || group.FeeValue != 0
        || group.PaymentValue != 0
        || group.PrePaymentValue != 0
        || CalculateRecapUnPaidIncome(group) != 0
        || CalculateRecapPaidOwnerRent(group) != 0
        || group.OwnerRentValue != 0
        || group.OwnerExpenseValue != 0
        || CalculateRecapOwnerPayment(group) != 0;

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

    private static bool IsRecapInvoiceSourceDocumentCode(string code)
    {
        var trimmed = (code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        return InvoiceSourceDocumentCodePattern.IsMatch(trimmed);
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

    private static readonly Regex RecapSourceCodePattern = new(@"\b(?:WO-[A-Za-z0-9-]+|R-\d+(?:-\d+)*|RC[A-Za-z0-9-]*)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RecapMemoSourceCodePattern = new(@"(?:Payment|Prepayment|Invoice)\s*:\s*([A-Za-z0-9-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ReservationCodePattern = new(@"^R-\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex InvoiceSourceDocumentCodePattern = new(@"^R-\d+-\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, int> SourcePriorityByCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ExpectedIncome"] = 100,
        ["PrePayment"] = 90,
        ["OwnerRent"] = 80,
        ["Payment"] = 60,
        ["RentPlus4000"] = 55,
        ["Expense"] = 50
    };

    #endregion
}

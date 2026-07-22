using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{

    public async Task<RecapReport> GetJournalEntryRecapReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        var recapLineSet = await LoadRecapLinesAsync(criteria, includePaymentInvoiceContext: true);
        return new RecapReport
        {
            Rows = BuildRecapReportRows(recapLineSet.AllLines)
        };
    }

    #region Build

    private static List<RecapReportRow> BuildRecapReportRows(IEnumerable<JournalEntryRecapLine> lines)
    {
        // AGENT-NOTE: DO NOT CHANGE recap rollup key.
        // Rows roll up strictly by SourceId + AccountingPeriod only — not property/reservation (even manual JEs).
        // SourceDocumentCode is display-only — the human-readable label for SourceId.
        var lineList = (lines ?? []).ToList();
        var groups = new Dictionary<string, GroupAccumulator>(StringComparer.OrdinalIgnoreCase);
        var propertyLevelExpenseGroups = new List<GroupAccumulator>();
        var prePayAppliedByKey = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var prePayReceivedByKey = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lineList)
        {
            var category = (line.RecapCategory ?? string.Empty).Trim();
            var amount = line.Amount;
            var reservationKey = GetReservationKey(line);

            if (string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase))
            {
                TrackRecapPrePaymentAmount(line, amount, prePayReceivedByKey, prePayAppliedByKey);
                if (amount <= 0)
                    continue;
            }

            if (string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(reservationKey))
            {
                propertyLevelExpenseGroups.Add(BuildRecapPropertyLevelExpenseGroup(line, amount));
                continue;
            }

            var recapGroup = GetOrCreateGroup(groups, line, category);
            if (line.IsInDateRange
                || string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "OwnerRentActual", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "ExpectedIncome", StringComparison.OrdinalIgnoreCase))
                recapGroup.HasInDateRangeLine = true;

            if (!string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase))
                RollupRecapCategoryAmount(recapGroup, category, amount);

            RollupRecapPrimaryJournalEntry(recapGroup, line, category);
            RollupRecapCategoryJournalEntryDetails(recapGroup, line, category);
            RollupRecapSourceDocument(recapGroup, line, category);
            RollupRecapEarliestTransactionDate(recapGroup, line);
        }

        ApplyRecapPrePaymentBalances(groups, prePayReceivedByKey, prePayAppliedByKey);

        var filteredGroups = groups.Values
            .Concat(propertyLevelExpenseGroups)
            .Where(group => group.HasInDateRangeLine && HasMeaningfulAmount(group))
            .ToList();

        return filteredGroups
            .OrderBy(group => group.AccountingPeriod, StringComparer.Ordinal)
            .ThenBy(group => group.PropertyCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.ReservationCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.SortDateValue)
            .ThenBy(group => group.JournalEntryLineId)
            .Select(BuildRecapReportRow)
            .ToList();
    }

    private static RecapReportRow BuildRecapReportRow(GroupAccumulator group)
    {
        var ownerRentValue = group.OwnerRentValue;
        var ownerRentActualValue = group.OwnerRentActualValue;
        // OwnPay is a forecast (OwnAct − OwnExp), not actual owner payout JEs.
        var ownerPaymentValue = ownerRentActualValue - group.OwnerExpenseValue;
        // UnRec = owner portion of period-2 OwnRent held in PrePay (receive period only). Not OwnRent − OwnAct.
        var ownerUnrecValue = group.PrePayOwnerUnpaidValue;
        var prepayAppliedCredit = group.PrePaymentValue < 0 ? Math.Abs(group.PrePaymentValue) : 0m;
        var tenantUnpaidValue = CalculateUnpaidIncome(group.ExpectedIncomeValue, group.PaymentValue + prepayAppliedCredit);
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
            UnPaid = FormatCurrencyUsd(tenantUnpaidValue),
            OwnerUnrec = FormatCurrencyUsd(ownerUnrecValue),
            OwnerRent = FormatCurrencyUsd(ownerRentValue),
            OwnerRentActual = FormatCurrencyUsd(ownerRentActualValue),
            OwnerExpense = FormatCurrencyUsd(group.OwnerExpenseValue),
            OwnerPayment = FormatCurrencyUsd(ownerPaymentValue),
            ExpectedIncomeValue = group.ExpectedIncomeValue,
            RentPlus4000Value = group.RentPlus4000Value,
            SecurityDepositValue = group.SecurityDepositValue,
            SdwValue = group.SdwValue,
            FeeValue = group.FeeValue,
            PaymentValue = group.PaymentValue,
            PrePaymentValue = group.PrePaymentValue,
            UnPaidValue = tenantUnpaidValue,
            OwnerUnrecValue = ownerUnrecValue,
            OwnerRentValue = ownerRentValue,
            OwnerRentActualValue = ownerRentActualValue,
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
            PostingStatusId = group.PostingStatusId
        };
    }

    private static GroupAccumulator BuildRecapPropertyLevelExpenseGroup(JournalEntryRecapLine line, decimal amount)
    {
        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
        var group = new GroupAccumulator
        {
            PropertyCode = (line.PropertyCode ?? string.Empty).Trim(),
            ReservationCode = (line.ReservationCode ?? string.Empty).Trim(),
            PropertyKey = GetPropertyKey(line),
            RollupSourceKey = GetRecapRollupSourceKey(line),
            PropertyId = (line.PropertyId?.ToString() ?? string.Empty).Trim(),
            ReservationId = (line.ReservationId?.ToString() ?? string.Empty).Trim(),
            OfficeId = line.OfficeId,
            AccountingPeriod = periodKey,
            TransactionDate = line.TransactionDate.ToString("yyyy-MM-dd"),
            SortDateValue = line.TransactionDate.ToDateTime(TimeOnly.MinValue).Ticks,
            JournalEntryId = line.JournalEntryId,
            JournalEntryLineId = line.JournalEntryLineId,
            HasInDateRangeLine = line.IsInDateRange
        };

        const string category = "Expense";
        RollupRecapCategoryAmount(group, category, amount);
        RollupRecapPrimaryJournalEntry(group, line, category);
        RollupRecapCategoryJournalEntryDetails(group, line, category);
        RollupRecapSourceDocument(group, line, category);
        RollupRecapEarliestTransactionDate(group, line);
        return group;
    }

    private static void TrackRecapPrePaymentAmount(
        JournalEntryRecapLine line,
        decimal amount,
        Dictionary<string, decimal> prePayReceivedByKey,
        Dictionary<string, decimal> prePayAppliedByKey)
    {
        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
        var rollupSource = GetRecapRollupSourceKey(line);
        if (string.IsNullOrWhiteSpace(rollupSource) || string.IsNullOrWhiteSpace(periodKey))
            return;

        var bucketKey = BuildPrePayBucketKey(rollupSource, periodKey);
        if (amount > 0)
            prePayReceivedByKey[bucketKey] = prePayReceivedByKey.GetValueOrDefault(bucketKey) + amount;
        else if (amount < 0)
            prePayAppliedByKey[bucketKey] = prePayAppliedByKey.GetValueOrDefault(bucketKey) + Math.Abs(amount);
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
            case "OwnerRentActual":
                group.OwnerRentActualValue += amount;
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
        group.PostingStatusId = line.PostingStatusId;
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

    #region PrePayment

    private static void ApplyRecapPrePaymentBalances(
        Dictionary<string, GroupAccumulator> groups,
        IReadOnlyDictionary<string, decimal> prePayReceivedByKey,
        IReadOnlyDictionary<string, decimal> prePayAppliedByKey)
    {
        var rollupSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in prePayReceivedByKey.Keys.Concat(prePayAppliedByKey.Keys))
        {
            var (rollupSource, _) = ParsePrePayBucketKey(key);
            if (!string.IsNullOrWhiteSpace(rollupSource))
                rollupSources.Add(rollupSource);
        }

        foreach (var rollupSource in rollupSources.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
        {
            var runningBalance = 0m;
            var sortedPeriods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups.Values.Where(group => RecapRollupSourceMatches(group, rollupSource)))
            {
                if (!string.IsNullOrWhiteSpace(group.AccountingPeriod))
                    sortedPeriods.Add(group.AccountingPeriod);
            }

            foreach (var key in prePayReceivedByKey.Keys.Concat(prePayAppliedByKey.Keys))
            {
                var (keyRollupSource, periodKey) = ParsePrePayBucketKey(key);
                if (!string.Equals(keyRollupSource, rollupSource, StringComparison.OrdinalIgnoreCase))
                    continue;

                sortedPeriods.Add(periodKey);
            }

            foreach (var period in sortedPeriods.OrderBy(value => value, StringComparer.Ordinal))
            {
                var bucketKey = BuildPrePayBucketKey(rollupSource, period);
                var receivedAmount = prePayReceivedByKey.GetValueOrDefault(bucketKey);
                var appliedAmount = prePayAppliedByKey.GetValueOrDefault(bucketKey);
                runningBalance += receivedAmount;

                var matchingGroups = groups.Values
                    .Where(group =>
                        string.Equals(group.AccountingPeriod, period, StringComparison.OrdinalIgnoreCase)
                        && RecapRollupSourceMatches(group, rollupSource))
                    .OrderBy(group => group.SortDateValue)
                    .ToList();

                if (receivedAmount > 0)
                {
                    foreach (var group in matchingGroups)
                        group.PrePaymentValue = runningBalance;

                    var applyPeriod = FindPrePayOwnerSharePeriod(
                        groups,
                        prePayAppliedByKey,
                        rollupSource,
                        period,
                        receivedAmount);
                    if (!string.IsNullOrWhiteSpace(applyPeriod))
                    {
                        var ownerUnpaidShare = CalculatePrePayOwnerShare(
                            receivedAmount,
                            groups.Values,
                            rollupSource,
                            applyPeriod);
                        foreach (var group in matchingGroups)
                            group.PrePayOwnerUnpaidValue += ownerUnpaidShare;
                    }
                }

                if (appliedAmount > 0)
                {
                    runningBalance -= appliedAmount;
                    if (runningBalance < 0)
                        runningBalance = 0;

                    foreach (var group in matchingGroups.Where(group => group.ExpectedIncomeValue != 0 || group.OwnerRentValue != 0))
                        group.PrePaymentValue = -appliedAmount;
                }
            }
        }
    }

    private static bool RecapRollupSourceMatches(GroupAccumulator group, string rollupSource)
    {
        var normalizedRollupSource = (rollupSource ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedRollupSource))
            return false;

        if (string.Equals(group.RollupSourceKey, normalizedRollupSource, StringComparison.OrdinalIgnoreCase))
            return true;

        return Guid.TryParse(normalizedRollupSource, out var rollupSourceId)
            && group.SourceId.HasValue
            && group.SourceId.Value == rollupSourceId;
    }

    private static (string RollupSource, string PeriodKey) ParsePrePayBucketKey(string bucketKey)
    {
        var parts = (bucketKey ?? string.Empty).Split('|');
        if (parts.Length < 2)
            return (string.Empty, string.Empty);

        return (parts[0], parts[1]);
    }

    private static string BuildPrePayBucketKey(string rollupSource, string periodKey)
        => $"{(rollupSource ?? string.Empty).Trim()}|{periodKey}";

    private static string? FindPrePayApplyPeriod(
        IReadOnlyDictionary<string, decimal> prePayAppliedByKey,
        string rollupSource,
        string afterPeriod)
    {
        string? applyPeriod = null;
        foreach (var key in prePayAppliedByKey.Keys)
        {
            var (keyRollupSource, periodKey) = ParsePrePayBucketKey(key);
            if (!string.Equals(keyRollupSource, rollupSource, StringComparison.OrdinalIgnoreCase))
                continue;
            if (prePayAppliedByKey.GetValueOrDefault(key) <= 0)
                continue;
            if (string.CompareOrdinal(periodKey, afterPeriod) <= 0)
                continue;

            if (applyPeriod == null || string.CompareOrdinal(periodKey, applyPeriod) < 0)
                applyPeriod = periodKey;
        }

        return applyPeriod;
    }

    private static string? FindPrePayOwnerSharePeriod(
        Dictionary<string, GroupAccumulator> groups,
        IReadOnlyDictionary<string, decimal> prePayAppliedByKey,
        string rollupSource,
        string receivePeriod,
        decimal receivedAmount)
    {
        var applyPeriod = FindPrePayApplyPeriod(prePayAppliedByKey, rollupSource, receivePeriod);
        if (!string.IsNullOrWhiteSpace(applyPeriod))
            return applyPeriod;

        if (receivedAmount <= 0)
            return null;

        return groups.Values
            .Where(group =>
                RecapRollupSourceMatches(group, rollupSource)
                && string.CompareOrdinal(group.AccountingPeriod, receivePeriod) > 0
                && group.ExpectedIncomeValue > 0
                && group.OwnerRentValue > 0)
            .OrderBy(group => group.AccountingPeriod, StringComparer.Ordinal)
            .Select(group => group.AccountingPeriod)
            .FirstOrDefault();
    }

    private static decimal CalculatePrePayOwnerShare(
        decimal prepayAmount,
        IEnumerable<GroupAccumulator> groups,
        string rollupSource,
        string ownerSharePeriod)
    {
        var applyPeriodGroup = groups
            .Where(group =>
                string.Equals(group.AccountingPeriod, ownerSharePeriod, StringComparison.OrdinalIgnoreCase)
                && RecapRollupSourceMatches(group, rollupSource))
            .FirstOrDefault(group => group.ExpectedIncomeValue > 0 && group.OwnerRentValue > 0)
            ?? groups
                .Where(group =>
                    string.Equals(group.AccountingPeriod, ownerSharePeriod, StringComparison.OrdinalIgnoreCase)
                    && RecapRollupSourceMatches(group, rollupSource))
                .FirstOrDefault(group => group.OwnerRentValue > 0);

        if (applyPeriodGroup == null || prepayAmount <= 0 || applyPeriodGroup.OwnerRentValue <= 0)
            return 0;

        // Owner portion of the prepaid slice = OwnRent on the apply-period row (from the agreement).
        if (applyPeriodGroup.ExpectedIncomeValue > 0
            && prepayAmount >= applyPeriodGroup.ExpectedIncomeValue)
            return applyPeriodGroup.OwnerRentValue;

        if (applyPeriodGroup.ExpectedIncomeValue <= 0)
            return 0;

        return Math.Min(
            applyPeriodGroup.OwnerRentValue,
            Math.Round(
                prepayAmount * applyPeriodGroup.OwnerRentValue / applyPeriodGroup.ExpectedIncomeValue,
                2,
                MidpointRounding.AwayFromZero));
    }

    #endregion

    #region Get

    private static GroupAccumulator GetOrCreateGroup(
        Dictionary<string, GroupAccumulator> groups,
        JournalEntryRecapLine line,
        string category)
    {
        var propertyKey = GetPropertyKey(line);
        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
        var rollupSourceKey = GetRecapRollupSourceKey(line);
        var groupKey = GetRecapGroupKey(propertyKey, rollupSourceKey, periodKey, line, category);
        if (groups.TryGetValue(groupKey, out var group))
            return group;

        group = new GroupAccumulator
        {
            PropertyCode = (line.PropertyCode ?? string.Empty).Trim(),
            ReservationCode = (line.ReservationCode ?? string.Empty).Trim(),
            PropertyKey = propertyKey,
            RollupSourceKey = rollupSourceKey,
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

    private static string GetRecapGroupKey(
        string propertyKey,
        string rollupSourceKey,
        string periodKey,
        JournalEntryRecapLine line,
        string category)
    {
        if (string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase))
            return $"{propertyKey}|expense|{line.JournalEntryLineId:D}";

        return $"{rollupSourceKey}|{periodKey}";
    }

    private static string GetRecapRollupSourceKey(JournalEntryRecapLine line)
    {
        return line.SourceId is { } sourceId && sourceId != Guid.Empty
            ? sourceId.ToString("D")
            : string.Empty;
    }

    private static string GetPropertyKey(JournalEntryRecapLine line) =>
        (line.PropertyCode ?? line.PropertyId?.ToString() ?? string.Empty).Trim();

    private static string GetReservationKey(JournalEntryRecapLine line) =>
        (line.ReservationCode ?? line.ReservationId?.ToString() ?? string.Empty).Trim();

    private static string GetRecapSourceDocumentCode(JournalEntryRecapLine line)
    {
        var fromProc = (line.SourceDocumentCode ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(fromProc))
            return fromProc;

        var description = (line.Description ?? string.Empty).Trim();
        var memoCode = AccountingManager.TryParseMemoSourceCode(description);
        if (!string.IsNullOrWhiteSpace(memoCode))
            return memoCode;

        var documentCode = AccountingManager.TryParseDocumentSourceCodeFromMemo(description);
        if (!string.IsNullOrWhiteSpace(documentCode))
            return documentCode;

        return string.Empty;
    }

    private static IEnumerable<string> GetRecapPaymentInvoiceSourceCodes(JournalEntryRecapLine line)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var description = (line.Description ?? string.Empty).Trim();

        var memoCode = AccountingManager.TryParseMemoSourceCode(description);
        if (!string.IsNullOrWhiteSpace(memoCode) && IsRecapInvoiceSourceDocumentCode(memoCode))
            codes.Add(memoCode);

        var documentCode = AccountingManager.TryParseDocumentSourceCodeFromMemo(description);
        if (!string.IsNullOrWhiteSpace(documentCode) && IsRecapInvoiceSourceDocumentCode(documentCode))
            codes.Add(documentCode);

        var sourceDocumentCode = GetRecapSourceDocumentCode(line);
        if (IsRecapInvoiceSourceDocumentCode(sourceDocumentCode))
            codes.Add(sourceDocumentCode);

        return codes;
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

    #endregion

    #region Helpers

    private static decimal CalculateRecapTenantUnpaidValue(GroupAccumulator group)
    {
        var prepayAppliedCredit = group.PrePaymentValue < 0 ? Math.Abs(group.PrePaymentValue) : 0m;
        return CalculateUnpaidIncome(group.ExpectedIncomeValue, group.PaymentValue + prepayAppliedCredit);
    }

    private static bool HasMeaningfulAmount(GroupAccumulator group) =>
        group.ExpectedIncomeValue != 0
        || group.RentPlus4000Value != 0
        || group.SecurityDepositValue != 0
        || group.SdwValue != 0
        || group.FeeValue != 0
        || group.PaymentValue != 0
        || group.PrePaymentValue != 0
        || group.PrePayOwnerUnpaidValue != 0
        || CalculateRecapTenantUnpaidValue(group) != 0
        || group.OwnerRentActualValue != 0
        || group.OwnerRentValue != 0
        || group.OwnerExpenseValue != 0
        || group.OwnerPaymentReceivedValue != 0;

    private static bool IsRecapSourceLinkable(int? sourceTypeId, Guid? sourceId, string documentCode)
    {
        var normalizedCode = (documentCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
            return false;

        if (AccountingManager.TryParseDocumentSourceCodeFromMemo(normalizedCode) == normalizedCode)
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

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
        var lineList = (lines ?? []).ToList();
        var groups = new Dictionary<string, GroupAccumulator>(StringComparer.OrdinalIgnoreCase);
        var propertyLevelExpenseGroups = new List<GroupAccumulator>();
        var prePayAppliedByKey = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var prePayReceivedByKey = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var invoicePeriodsByLookupKey = BuildRecapInvoicePeriodsByLookupKey(lineList);

        foreach (var line in lineList)
        {
            var category = (line.RecapCategory ?? string.Empty).Trim();
            var amount = line.Amount;
            var reservationKey = GetReservationKey(line);
            var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");

            if (string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase))
            {
                if (amount > 0 && !string.IsNullOrWhiteSpace(reservationKey) && !string.IsNullOrWhiteSpace(periodKey))
                {
                    var invoiceSource = GetOwnerIncomeInvoiceSourceKey(line, category);
                    var receiveKey = BuildPrePayApplyKey(reservationKey, periodKey, invoiceSource);
                    prePayReceivedByKey[receiveKey] = prePayReceivedByKey.GetValueOrDefault(receiveKey) + amount;
                    var group = GetOrCreateGroup(groups, line, category, invoicePeriodsByLookupKey, forcePeriodGrouping: true);
                    if (line.IsInDateRange)
                        group.HasInDateRangeLine = true;
                    RollupRecapPrimaryJournalEntry(group, line, category);
                    RollupRecapCategoryJournalEntryDetails(group, line, category);
                    RollupRecapSourceDocument(group, line, category);
                    RollupRecapEarliestTransactionDate(group, line);
                }
                else if (amount < 0 && !string.IsNullOrWhiteSpace(reservationKey) && !string.IsNullOrWhiteSpace(periodKey))
                {
                    var invoiceSource = GetOwnerIncomeInvoiceSourceKey(line, category);
                    var applyKey = BuildPrePayApplyKey(reservationKey, periodKey, invoiceSource);
                    prePayAppliedByKey[applyKey] = prePayAppliedByKey.GetValueOrDefault(applyKey) + Math.Abs(amount);
                }

                continue;
            }

            if (string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(reservationKey))
            {
                propertyLevelExpenseGroups.Add(BuildRecapPropertyLevelExpenseGroup(line, amount));
                continue;
            }

            var recapGroup = GetOrCreateGroup(groups, line, category, invoicePeriodsByLookupKey);
            if (line.IsInDateRange
                || string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "ExpectedIncome", StringComparison.OrdinalIgnoreCase))
                recapGroup.HasInDateRangeLine = true;

            RollupRecapCategoryAmount(recapGroup, category, amount);
            RollupRecapPrimaryJournalEntry(recapGroup, line, category);
            RollupRecapCategoryJournalEntryDetails(recapGroup, line, category);
            RollupRecapSourceDocument(recapGroup, line, category);
            RollupRecapInvoiceAccountingPeriod(recapGroup, line, category);
            RollupRecapEarliestTransactionDate(recapGroup, line);
        }

        ApplyRecapPrePaymentBalances(groups, prePayReceivedByKey, prePayAppliedByKey);
        MergeRecapOwnerRentIntoInvoiceGroups(groups);

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
        var ownerPaymentValue = group.OwnerPaymentReceivedValue;
        var unPaidValue = CalculateUnpaidIncome(ownerRentValue, ownerRentActualValue);
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
            UnPaidValue = unPaidValue,
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

    private static void MergeRecapOwnerRentIntoInvoiceGroups(Dictionary<string, GroupAccumulator> groups)
    {
        foreach (var entry in groups.ToList())
        {
            var orphanGroup = entry.Value;
            if (orphanGroup.OwnerRentValue == 0 || orphanGroup.ExpectedIncomeValue != 0 || orphanGroup.PaymentValue != 0)
                continue;

            var invoiceSource = ResolveRecapOwnerRentInvoiceSource(orphanGroup);
            if (string.IsNullOrWhiteSpace(invoiceSource))
                continue;

            var targetGroup = groups.Values.FirstOrDefault(group =>
                group != orphanGroup
                && string.Equals(group.PropertyKey, orphanGroup.PropertyKey, StringComparison.OrdinalIgnoreCase)
                && string.Equals(group.ReservationKey, orphanGroup.ReservationKey, StringComparison.OrdinalIgnoreCase)
                && string.Equals(group.AccountingPeriod, orphanGroup.AccountingPeriod, StringComparison.OrdinalIgnoreCase)
                && string.Equals((group.SourceDocumentCode ?? string.Empty).Trim(), invoiceSource, StringComparison.OrdinalIgnoreCase)
                && (group.ExpectedIncomeValue != 0 || group.PaymentValue != 0 || group.RentPlus4000Value != 0));

            if (targetGroup == null)
                continue;

            targetGroup.OwnerRentValue += orphanGroup.OwnerRentValue;
            if (string.IsNullOrWhiteSpace(targetGroup.OwnerRentMemo) && !string.IsNullOrWhiteSpace(orphanGroup.OwnerRentMemo))
                targetGroup.OwnerRentMemo = orphanGroup.OwnerRentMemo;
            if (string.IsNullOrWhiteSpace(targetGroup.OwnerRentJournalEntryCode) && !string.IsNullOrWhiteSpace(orphanGroup.OwnerRentJournalEntryCode))
                targetGroup.OwnerRentJournalEntryCode = orphanGroup.OwnerRentJournalEntryCode;
            if (targetGroup.OwnerRentJournalEntryId == null)
                targetGroup.OwnerRentJournalEntryId = orphanGroup.OwnerRentJournalEntryId;
            if (targetGroup.OwnerRentJournalEntryLineId == null)
                targetGroup.OwnerRentJournalEntryLineId = orphanGroup.OwnerRentJournalEntryLineId;

            groups.Remove(entry.Key);
        }
    }

    private static string ResolveRecapOwnerRentInvoiceSource(GroupAccumulator group)
    {
        var sourceDocumentCode = (group.SourceDocumentCode ?? string.Empty).Trim();
        if (IsRecapInvoiceSourceDocumentCode(sourceDocumentCode))
            return sourceDocumentCode;

        return AccountingManager.TryParseInvoiceSourceCodeFromMemo(group.OwnerRentMemo)
            ?? AccountingManager.TryParseInvoiceSourceCodeFromMemo(group.Memo)
            ?? string.Empty;
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

    private static void RollupRecapInvoiceAccountingPeriod(GroupAccumulator group, JournalEntryRecapLine line, string category)
    {
        if (!ShouldSetRecapAccountingPeriodFromLine(category, line))
            return;

        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
        if (string.IsNullOrWhiteSpace(periodKey))
            return;

        var priority = SourcePriorityByCategory.GetValueOrDefault(category);
        if (!string.IsNullOrWhiteSpace(group.AccountingPeriod) && priority < group.AccountingPeriodPriority)
            return;

        group.AccountingPeriodPriority = priority;
        group.AccountingPeriod = periodKey;
    }

    #endregion

    #region PrePayment

    private static void ApplyRecapPrePaymentBalances(
        Dictionary<string, GroupAccumulator> groups,
        IReadOnlyDictionary<string, decimal> prePayReceivedByKey,
        IReadOnlyDictionary<string, decimal> prePayAppliedByKey)
    {
        var reservationInvoiceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in prePayReceivedByKey.Keys.Concat(prePayAppliedByKey.Keys))
        {
            var (reservationKey, _, invoiceSource) = ParsePrePayApplyKey(key);
            if (string.IsNullOrWhiteSpace(reservationKey) || string.IsNullOrWhiteSpace(invoiceSource))
                continue;

            reservationInvoiceKeys.Add($"{reservationKey}|{invoiceSource}");
        }

        foreach (var reservationInvoiceKey in reservationInvoiceKeys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
        {
            var separatorIndex = reservationInvoiceKey.IndexOf('|');
            if (separatorIndex <= 0)
                continue;

            var reservationKey = reservationInvoiceKey[..separatorIndex];
            var invoiceSource = reservationInvoiceKey[(separatorIndex + 1)..];
            var runningBalance = 0m;
            var sortedPeriods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups.Values)
            {
                if (!string.Equals(group.ReservationKey, reservationKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!RecapInvoiceSourceMatches(group, invoiceSource))
                    continue;
                if (!string.IsNullOrWhiteSpace(group.AccountingPeriod))
                    sortedPeriods.Add(group.AccountingPeriod);
            }

            foreach (var key in prePayReceivedByKey.Keys.Concat(prePayAppliedByKey.Keys))
            {
                var (keyReservation, periodKey, keyInvoiceSource) = ParsePrePayApplyKey(key);
                if (!string.Equals(keyReservation, reservationKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(keyInvoiceSource, invoiceSource, StringComparison.OrdinalIgnoreCase))
                    continue;

                sortedPeriods.Add(periodKey);
            }

            foreach (var period in sortedPeriods.OrderBy(value => value, StringComparer.Ordinal))
            {
                var bucketKey = BuildPrePayApplyKey(reservationKey, period, invoiceSource);
                var receivedAmount = prePayReceivedByKey.GetValueOrDefault(bucketKey);
                var appliedAmount = prePayAppliedByKey.GetValueOrDefault(bucketKey);
                runningBalance += receivedAmount;

                var matchingGroups = groups.Values
                    .Where(group =>
                        string.Equals(group.ReservationKey, reservationKey, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(group.AccountingPeriod, period, StringComparison.OrdinalIgnoreCase)
                        && RecapInvoiceSourceMatches(group, invoiceSource))
                    .OrderBy(group => group.ExpectedIncomeValue == 0 && group.OwnerRentValue == 0 ? 0 : 1)
                    .ThenBy(group => group.SortDateValue)
                    .ToList();

                if (appliedAmount > 0)
                {
                    var invoiceGroup = matchingGroups
                        .FirstOrDefault(group => group.ExpectedIncomeValue != 0 || group.OwnerRentValue != 0);
                    if (invoiceGroup != null)
                        invoiceGroup.PaymentValue += appliedAmount;

                    runningBalance -= appliedAmount;
                    if (runningBalance < 0)
                        runningBalance = 0;
                }

                if (receivedAmount > 0)
                {
                    var receiveGroup = matchingGroups
                        .FirstOrDefault(group => group.ExpectedIncomeValue == 0 && group.OwnerRentValue == 0)
                        ?? matchingGroups.FirstOrDefault();
                    if (receiveGroup != null)
                        receiveGroup.PrePaymentValue = runningBalance;
                }
                else if (appliedAmount > 0)
                {
                    foreach (var group in matchingGroups.Where(group => group.ExpectedIncomeValue != 0 || group.OwnerRentValue != 0))
                        group.PrePaymentValue = runningBalance;
                }
            }
        }
    }

    private static bool RecapInvoiceSourceMatches(GroupAccumulator group, string invoiceSource)
    {
        var normalizedInvoiceSource = (invoiceSource ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedInvoiceSource))
            return true;

        return string.Equals((group.SourceDocumentCode ?? string.Empty).Trim(), normalizedInvoiceSource, StringComparison.OrdinalIgnoreCase);
    }

    private static (string ReservationKey, string PeriodKey, string InvoiceSource) ParsePrePayApplyKey(string applyKey)
    {
        var parts = (applyKey ?? string.Empty).Split('|');
        if (parts.Length < 3)
            return (string.Empty, string.Empty, string.Empty);

        return (parts[0], parts[1], parts[2]);
    }

    private static string BuildPrePayApplyKey(string reservationKey, string periodKey, string invoiceSourceKey)
        => $"{reservationKey}|{periodKey}|{(invoiceSourceKey ?? string.Empty).Trim()}";

    #endregion

    #region Get

    private static GroupAccumulator GetOrCreateGroup(
        Dictionary<string, GroupAccumulator> groups,
        JournalEntryRecapLine line,
        string category,
        IReadOnlyDictionary<string, HashSet<string>> invoicePeriodsByLookupKey,
        bool forcePeriodGrouping = false)
    {
        var propertyKey = GetPropertyKey(line);
        var reservationKey = GetReservationKey(line);
        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
        var groupKey = forcePeriodGrouping
            ? GetGroupKey(propertyKey, reservationKey, periodKey)
            : GetRecapGroupKey(line, category, invoicePeriodsByLookupKey);
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
            AccountingPeriod = forcePeriodGrouping
                ? periodKey
                : GetRecapGroupPeriodKey(line, category, invoicePeriodsByLookupKey),
            TransactionDate = line.TransactionDate.ToString("yyyy-MM-dd"),
            SortDateValue = line.TransactionDate.ToDateTime(TimeOnly.MinValue).Ticks
        };
        groups[groupKey] = group;
        return group;
    }

    private static string GetRecapGroupKey(
        JournalEntryRecapLine line,
        string category,
        IReadOnlyDictionary<string, HashSet<string>> invoicePeriodsByLookupKey)
    {
        var propertyKey = GetPropertyKey(line);
        var reservationKey = GetReservationKey(line);
        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");

        if (ShouldGroupRecapByInvoiceSource(category, line))
        {
            var invoiceSourceKey = GetOwnerIncomeInvoiceSourceKey(line, category);
            if (!string.IsNullOrWhiteSpace(invoiceSourceKey))
            {
                var groupPeriodKey = GetRecapGroupPeriodKey(line, category, invoicePeriodsByLookupKey);
                return $"{propertyKey}|{reservationKey}|inv:{invoiceSourceKey}|{groupPeriodKey}";
            }
        }

        return GetGroupKey(propertyKey, reservationKey, periodKey);
    }

    private static string GetRecapGroupPeriodKey(
        JournalEntryRecapLine line,
        string category,
        IReadOnlyDictionary<string, HashSet<string>> invoicePeriodsByLookupKey)
    {
        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
        if (!ShouldGroupRecapByInvoiceSource(category, line))
            return periodKey;

        if (!string.Equals(category, "Payment", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(category, "OwnerRentActual", StringComparison.OrdinalIgnoreCase)
            && !(string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase) && line.Amount < 0))
        {
            return periodKey;
        }

        var invoiceSourceKey = GetOwnerIncomeInvoiceSourceKey(line, category);
        if (string.IsNullOrWhiteSpace(invoiceSourceKey))
            return periodKey;

        var lookupKey = GetRecapInvoicePeriodLookupKey(line, invoiceSourceKey);
        if (invoicePeriodsByLookupKey.TryGetValue(lookupKey, out var invoicePeriods)
            && invoicePeriods.Count == 1)
        {
            return invoicePeriods.First();
        }

        return periodKey;
    }

    private static Dictionary<string, HashSet<string>> BuildRecapInvoicePeriodsByLookupKey(IEnumerable<JournalEntryRecapLine> lines)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var category = (line.RecapCategory ?? string.Empty).Trim();
            if (!string.Equals(category, "ExpectedIncome", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var invoiceSourceKey = GetOwnerIncomeInvoiceSourceKey(line, category);
            if (string.IsNullOrWhiteSpace(invoiceSourceKey))
                continue;

            var lookupKey = GetRecapInvoicePeriodLookupKey(line, invoiceSourceKey);
            var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
            if (!result.TryGetValue(lookupKey, out var periods))
            {
                periods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                result[lookupKey] = periods;
            }

            periods.Add(periodKey);
        }

        return result;
    }

    private static string GetRecapInvoicePeriodLookupKey(JournalEntryRecapLine line, string invoiceSourceKey)
    {
        var propertyKey = GetPropertyKey(line);
        var reservationKey = GetReservationKey(line);
        return $"{propertyKey}|{reservationKey}|inv:{invoiceSourceKey}";
    }

    private static bool ShouldGroupRecapByInvoiceSource(string category, JournalEntryRecapLine line)
    {
        if (string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase))
            return line.Amount < 0;

        return category is "ExpectedIncome" or "OwnerRent" or "OwnerRentActual" or "Payment" or "OwnerPayment"
            or "RentPlus4000" or "SecurityDeposit" or "SDW" or "Fee";
    }

    private static bool ShouldSetRecapAccountingPeriodFromLine(string category, JournalEntryRecapLine line)
    {
        if (string.Equals(category, "Payment", StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase) && line.Amount > 0)
            return false;

        return ShouldGroupRecapByInvoiceSource(category, line);
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
        var memoCode = AccountingManager.TryParseMemoSourceCode(description);
        if (!string.IsNullOrWhiteSpace(memoCode))
            return memoCode;

        var documentCode = AccountingManager.TryParseDocumentSourceCodeFromMemo(description);
        if (!string.IsNullOrWhiteSpace(documentCode))
            return documentCode;

        if (line.SourceTypeId is (int)SourceType.Invoice or (int)SourceType.InvoicePayment
            || string.Equals(line.RecapCategory, "ExpectedIncome", StringComparison.OrdinalIgnoreCase)
            || string.Equals(line.RecapCategory, "OwnerRent", StringComparison.OrdinalIgnoreCase)
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
        || CalculateUnpaidIncome(group.OwnerRentValue, group.OwnerRentActualValue) != 0
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

using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using System.Globalization;

namespace RentAll.Domain.Managers;

public partial class ReportManager : IReportManager
{
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IAccountingRepository _accountingRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IAccountingManager _accountingManager;

    private const string OwnerStartingBalanceMemoPrefix = "Owner: Starting Balance:";

    public ReportManager(IJournalEntryRepository journalEntryRepository, IAccountingRepository accountingRepository, IOrganizationRepository organizationRepository, IPropertyRepository propertyRepository, IAccountingManager accountingManager)
    {
        _journalEntryRepository = journalEntryRepository;
        _accountingRepository = accountingRepository;
        _organizationRepository = organizationRepository;
        _propertyRepository = propertyRepository;
        _accountingManager = accountingManager;
    }

    #region Load
    private async Task<RecapLineSet> LoadRecapLinesAsync(JournalEntryRecapGetCriteria criteria, bool includePaymentInvoiceContext)
    {
        criteria.IncludePaymentInvoiceContext = includePaymentInvoiceContext;
        var allLines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(criteria)).ToList();
        return new RecapLineSet
        {
            AllLines = allLines,
            ActivityLines = allLines.Where(line => line.IsInDateRange).ToList()
        };
    }

    private async Task<List<PropertyReportData>> LoadOwnerPropertyReportDataAsync(JournalEntryRecapGetCriteria criteria)
    {
        var properties = (await _propertyRepository.GetPropertyReportDataAsync(criteria.OrganizationId, criteria.OfficeIds, criteria.PropertyId)).ToList();

        return properties
            .Where(property => property.PropertyLeaseType == PropertyLeaseType.PropertyManagement)
            .Where(property => property.PrimaryOwnerId.HasValue && property.PrimaryOwnerId.Value != Guid.Empty)
            .OrderBy(property => property.OfficeName)
            .ThenBy(property => property.PropertyCode)
            .ToList();
    }

    private async Task<Dictionary<string, OwnerStartingBalance>> LoadOwnerStartingBalanceByPropertyAsync(JournalEntryRecapGetCriteria criteria, IReadOnlyList<int> officeIds)
    {
        var startingBalanceByKey = new Dictionary<string, OwnerStartingBalance>(StringComparer.OrdinalIgnoreCase);
        var priorMonthClose = GetPriorMonthCloseDate(criteria.StartDate, criteria.EndDate);
        var periodStart = GetReportPeriodStartDate(criteria.StartDate, criteria.EndDate);
        if (!priorMonthClose.HasValue && !periodStart.HasValue)
            return startingBalanceByKey;

        foreach (var officeId in officeIds)
        {
            var chartOfAccounts = (await _accountingRepository.GetChartOfAccountsByOfficeIdAsync(criteria.OrganizationId, officeId)).ToList();
            var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(criteria.OrganizationId, officeId);
            var ownerAccountsPayableAccountId = _accountingManager.GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice);

            if (priorMonthClose.HasValue)
            {
                var priorMonthOwnerApLines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
                {
                    OrganizationId = criteria.OrganizationId,
                    OfficeIds = officeId.ToString(),
                    ChartOfAccountId = ownerAccountsPayableAccountId,
                    PropertyId = criteria.PropertyId,
                    StartDate = null,
                    EndDate = priorMonthClose,
                    IncludeVoided = false,
                    IncludeUnposted = true
                });

                foreach (var group in priorMonthOwnerApLines.Where(line => line.PropertyId.HasValue && line.PropertyId.Value != Guid.Empty).GroupBy(line => GetPropertyReportKey(line.OfficeId, line.PropertyId!.Value)))
                {
                    startingBalanceByKey[group.Key] = CalculateOwnerStartingBalance(group);
                }
            }

            if (periodStart.HasValue)
            {
                var reportEnd = GetReportPeriodEndDate(criteria.StartDate, criteria.EndDate);
                if (reportEnd.HasValue)
                {
                    await LoadOwnerStartingBalanceInReportRangeAsync(criteria, officeId, ownerAccountsPayableAccountId, periodStart.Value, reportEnd.Value, startingBalanceByKey);
                }
            }
        }

        return startingBalanceByKey;
    }

    private async Task LoadOwnerStartingBalanceInReportRangeAsync(JournalEntryRecapGetCriteria criteria, int officeId, int ownerAccountsPayableAccountId, DateOnly periodStart, DateOnly reportEnd, Dictionary<string, OwnerStartingBalance> startingBalanceByKey)
    {
        var inRangeOwnerApLines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = officeId.ToString(),
            ChartOfAccountId = ownerAccountsPayableAccountId,
            PropertyId = criteria.PropertyId,
            StartDate = periodStart,
            EndDate = reportEnd,
            IncludeVoided = false,
            IncludeUnposted = true
        });

        foreach (var propertyGroup in inRangeOwnerApLines.Where(line => line.PropertyId.HasValue && line.PropertyId.Value != Guid.Empty).Where(line => IsOwnerStartingBalanceMemo(line.JournalEntryMemo, line.Memo)).GroupBy(line => GetPropertyReportKey(line.OfficeId, line.PropertyId!.Value)))
        {
            if (startingBalanceByKey.TryGetValue(propertyGroup.Key, out var existingSnapshot)
                && existingSnapshot.LedgerBalance != 0)
            {
                continue;
            }

            var earliestStartingBalanceEntry = propertyGroup
                .GroupBy(line => line.JournalEntryId)
                .Select(journalEntryGroup =>
                {
                    var firstLine = journalEntryGroup.First();
                    return new
                    {
                        firstLine.TransactionDate,
                        firstLine.JournalEntryCode,
                        NetBalance = journalEntryGroup.Sum(line => line.Credit - line.Debit)
                    };
                })
                .Where(entry => entry.NetBalance != 0)
                .OrderBy(entry => entry.TransactionDate)
                .ThenBy(entry => entry.JournalEntryCode)
                .FirstOrDefault();

            if (earliestStartingBalanceEntry == null)
                continue;

            startingBalanceByKey[propertyGroup.Key] = new OwnerStartingBalance
            {
                LedgerBalance = earliestStartingBalanceEntry.NetBalance,
                OpeningAccountsPayableAmount = earliestStartingBalanceEntry.NetBalance,
                OpeningBalanceTransactionDate = earliestStartingBalanceEntry.TransactionDate
            };
        }
    }

    #endregion

    #region Build
    private static List<OwnerStatementPropertyActivityLine> BuildOwnerActivityLines(IEnumerable<JournalEntryRecapLine>? activityLines, IEnumerable<JournalEntryRecapLine>? invoiceOwnerIncomeLines, OwnerReportActivityMode mode)
    {
        activityLines ??= [];
        invoiceOwnerIncomeLines ??= [];
        var allLines = activityLines.Concat(invoiceOwnerIncomeLines).ToList();
        var invoiceOwnerIncomeByKey = GetInvoiceOwnerIncomeTotalsByInvoiceKey(invoiceOwnerIncomeLines);
        var prepaymentPaymentSourceIds = GetPrepaymentPaymentSourceIds(allLines);
        var invoiceAccountingPeriodByKey = GetInvoiceAccountingPeriodByInvoiceKey(allLines);
        var groups = new Dictionary<string, OwnerInvoiceActivityGroup>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in activityLines)
        {
            if (!TryGetRecapLinePropertyId(line, out var propertyId))
                continue;

            var category = (line.RecapCategory ?? string.Empty).Trim();
            if (!IsOwnerReportRecapCategory(category))
                continue;

            if (string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase) && line.Amount > 0)
                continue;

            if (IsPrepaymentPaymentRecapLine(line, prepaymentPaymentSourceIds, invoiceAccountingPeriodByKey))
                continue;

            var groupKey = GetOwnerInvoiceActivityGroupKey(line, category);
            if (!groups.TryGetValue(groupKey, out var group))
            {
                var invoiceSourceCode = GetOwnerIncomeInvoiceSourceKey(line, category);
                group = new OwnerInvoiceActivityGroup
                {
                    PropertyId = propertyId,
                    OfficeId = line.OfficeId,
                    InvoiceSourceCode = invoiceSourceCode,
                    AccountingPeriod = line.AccountingPeriod.ToString("yyyy-MM-dd"),
                    SourceDocumentCode = GetRecapSourceDocumentCode(line),
                    TransactionDate = line.TransactionDate.ToString("yyyy-MM-dd"),
                    SortDateValue = line.TransactionDate.ToDateTime(TimeOnly.MinValue).Ticks
                };
                groups[groupKey] = group;
            }

            RollupAmountsForOwnerInvoiceGroup(group, line, category);
            RollupOwnerInvoiceGroupBySourceAndDates(group, line);
        }

        var activityRows = groups.Values
            .Where(IsOwnerInvoiceGroupWithActivity)
            .SelectMany(group => BuildOwnerActivityLinesFromInvoiceGroup(group, GetInvoiceOwnerIncomeKey(group.PropertyId, group.InvoiceSourceCode ?? group.SourceDocumentCode), invoiceOwnerIncomeByKey, mode))
            .ToList();

        if (mode == OwnerReportActivityMode.Accrual)
            activityRows.AddRange(BuildOwnerActivityLinesFromPrepaidReceive(activityLines));

        var ordered = activityRows
            .OrderBy(line => line.OfficeId)
            .ThenBy(line => line.PropertyId);

        if (mode == OwnerReportActivityMode.Cash)
        {
            return ordered
                .ThenBy(line => line.ActivityDate)
                .ThenBy(line => GetOwnerActivityAccountingPeriodSortKey(line.AccountingPeriod))
                .ThenBy(line => GetOwnerActivityLineSortOrder(line))
                .ThenBy(line => line.DocumentCode, StringComparer.Ordinal)
                .ToList();
        }

        return ordered
            .ThenBy(line => GetOwnerActivityAccountingPeriodSortKey(line.AccountingPeriod))
            .ThenBy(line => line.ActivityDate)
            .ThenBy(line => GetOwnerActivityLineSortOrder(line))
            .ThenBy(line => line.DocumentCode, StringComparer.Ordinal)
            .ToList();
    }

    private static Dictionary<string, List<OwnerStatementPropertyActivityLine>> BuildOwnerActivityLinesByProperty(IEnumerable<OwnerStatementPropertyActivityLine> lines)
    {
        return lines
            .GroupBy(line => GetPropertyReportKey(line.OfficeId, line.PropertyId))
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<OwnerStatementPropertyActivityLine> BuildOwnerActivityLinesFromPrepaidReceive(IEnumerable<JournalEntryRecapLine>? activityLines)
    {
        foreach (var line in activityLines ?? [])
        {
            if (!TryGetRecapLinePropertyId(line, out var propertyId))
                continue;

            var category = (line.RecapCategory ?? string.Empty).Trim();
            if (!string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase) || line.Amount <= 0)
                continue;

            var sourceDocumentCode = GetRecapSourceDocumentCode(line);
            yield return new OwnerStatementPropertyActivityLine
            {
                PropertyId = propertyId,
                OfficeId = line.OfficeId,
                ActivityId = line.JournalEntryLineId,
                SourceId = line.SourceId,
                JournalEntryLineId = line.JournalEntryLineId,
                ActivityType = GetRecapActivityType(line.SourceTypeId, sourceDocumentCode),
                ActivityDate = line.TransactionDate,
                AccountingPeriod = FormatJournalEntryRecapAccountingPeriod(line.AccountingPeriod.ToString("yyyy-MM-dd")),
                DocumentCode = (line.JournalEntryCode ?? string.Empty).Trim(),
                SourceDocumentCode = sourceDocumentCode,
                Description = StripTenantPaymentMemoForDisplay(line.Description ?? string.Empty),
                PrepaidIncome = line.Amount
            };
        }
    }

    private static IEnumerable<OwnerStatementPropertyActivityLine> BuildOwnerActivityLinesFromInvoiceGroup(OwnerInvoiceActivityGroup group, string invoiceOwnerIncomeKey, IReadOnlyDictionary<string, InvoiceOwnerIncomeTotals> invoiceOwnerIncomeByKey, OwnerReportActivityMode mode)
    {
        invoiceOwnerIncomeByKey.TryGetValue(invoiceOwnerIncomeKey, out var invoiceContext);
        var paidIncome = GetOwnerPaidIncomeForInvoiceGroup(group, invoiceOwnerIncomeKey, invoiceOwnerIncomeByKey);
        var hasOwnerRentInGroup = group.OwnerRentValue != 0;
        var hasPaidIncome = paidIncome != 0;
        var isCrossPeriodPayment = !hasOwnerRentInGroup && hasPaidIncome && invoiceContext?.OwnerRentValue > 0;
        var isAccrual = mode == OwnerReportActivityMode.Accrual;
        var ownerRentAccountingPeriod = FormatJournalEntryRecapAccountingPeriod(group.OwnerRentAccountingPeriod ?? group.AccountingPeriod);
        var paymentAccountingPeriod = FormatJournalEntryRecapAccountingPeriod(group.AccountingPeriod);

        if (hasOwnerRentInGroup && hasPaidIncome)
        {
            yield return new OwnerStatementPropertyActivityLine
            {
                PropertyId = group.PropertyId,
                OfficeId = group.OfficeId,
                ActivityId = group.OwnerRentJournalEntryLineId,
                SourceId = group.OwnerRentSourceId,
                JournalEntryLineId = group.OwnerRentJournalEntryLineId,
                ActivityType = GetRecapActivityType(group.OwnerRentSourceTypeId, group.SourceDocumentCode),
                ActivityDate = ParseActivityDate(group.TransactionDate),
                AccountingPeriod = ownerRentAccountingPeriod,
                DocumentCode = GetOwnerRentDocumentCode(group),
                SourceDocumentCode = GetOwnerActivityRefNo(group),
                Description = GetOwnerRentActivityDescription(group),
                ExpectedIncome = isAccrual ? group.OwnerRentValue : 0,
                ReceivedIncome = paidIncome,
                Expenses = 0,
                OwnerPayment = 0
            };
        }
        else if (hasOwnerRentInGroup)
        {
            yield return new OwnerStatementPropertyActivityLine
            {
                PropertyId = group.PropertyId,
                OfficeId = group.OfficeId,
                ActivityId = group.OwnerRentJournalEntryLineId,
                SourceId = group.OwnerRentSourceId,
                JournalEntryLineId = group.OwnerRentJournalEntryLineId,
                ActivityType = GetRecapActivityType(group.OwnerRentSourceTypeId, group.SourceDocumentCode),
                ActivityDate = ParseActivityDate(group.TransactionDate),
                AccountingPeriod = ownerRentAccountingPeriod,
                DocumentCode = GetOwnerRentDocumentCode(group),
                SourceDocumentCode = GetOwnerActivityRefNo(group),
                Description = GetOwnerRentActivityDescription(group),
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
                ActivityId = isCrossPeriodPayment ? invoiceContext?.OwnerRentJournalEntryLineId ?? group.PaymentJournalEntryLineId : group.PaymentJournalEntryLineId ?? group.OwnerPaymentJournalEntryLineId,
                SourceId = isCrossPeriodPayment ? invoiceContext?.OwnerRentSourceId ?? group.PaymentSourceId : group.PaymentSourceId ?? group.OwnerPaymentSourceId,
                JournalEntryLineId = isCrossPeriodPayment ? invoiceContext?.OwnerRentJournalEntryLineId ?? group.PaymentJournalEntryLineId : group.PaymentJournalEntryLineId ?? group.OwnerPaymentJournalEntryLineId,
                ActivityType = GetRecapActivityType(isCrossPeriodPayment ? invoiceContext?.OwnerRentSourceTypeId ?? group.PaymentSourceTypeId : group.PaymentSourceTypeId ?? group.OwnerPaymentSourceTypeId, group.SourceDocumentCode),
                ActivityDate = ParseActivityDate(group.TransactionDate),
                AccountingPeriod = paymentAccountingPeriod,
                DocumentCode = isCrossPeriodPayment ? GetOwnerRentDocumentCode(group, invoiceContext) : GetTenantPaymentDocumentCode(group),
                SourceDocumentCode = GetOwnerActivityRefNo(group, invoiceContext),
                Description = isCrossPeriodPayment ? GetOwnerRentActivityDescription(group, invoiceContext) : GetTenantPaymentActivityDescription(group),
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
                ActivityDate = ParseActivityDate(group.TransactionDate),
                AccountingPeriod = FormatJournalEntryRecapAccountingPeriod(group.AccountingPeriod),
                DocumentCode = GetOwnerExpenseDocumentCode(group),
                SourceDocumentCode = GetOwnerActivityRefNo(group),
                Description = GetOwnerExpenseActivityDescription(group),
                ExpectedIncome = 0,
                ReceivedIncome = 0,
                Expenses = group.OwnerExpenseValue,
                OwnerPayment = 0
            };
        }
    }

    #endregion

    #region Rollup

    private static void RollupAmountsForOwnerInvoiceGroup(OwnerInvoiceActivityGroup group, JournalEntryRecapLine line, string category)
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

        if (string.Equals(category, "Payment", StringComparison.OrdinalIgnoreCase) || (string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase) && amount < 0))
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

    private static void RollupOwnerInvoiceGroupBySourceAndDates(OwnerInvoiceActivityGroup group, JournalEntryRecapLine line)
    {
        var transactionDate = line.TransactionDate.ToString("yyyy-MM-dd");
        if (string.IsNullOrWhiteSpace(transactionDate))
            return;

        if (string.IsNullOrWhiteSpace(group.TransactionDate) || string.CompareOrdinal(transactionDate, group.TransactionDate) < 0)
        {
            group.TransactionDate = transactionDate;
            group.SortDateValue = line.TransactionDate.ToDateTime(TimeOnly.MinValue).Ticks;
        }

        if (string.IsNullOrWhiteSpace(group.SourceDocumentCode))
            group.SourceDocumentCode = GetRecapSourceDocumentCode(line);

        if (!group.SourceId.HasValue && line.SourceId.HasValue)
            group.SourceId = line.SourceId;

        if (!group.SourceTypeId.HasValue && line.SourceTypeId.HasValue)
            group.SourceTypeId = line.SourceTypeId;
    }

    #endregion

    #region Calculate
    private static decimal CalculateOwnerPaidIncome(decimal ownerRent, decimal expectedIncome, decimal paymentAmount, decimal ownerPaymentReceived)
    {
        if (ownerPaymentReceived != 0)
            return ownerPaymentReceived;
        if (paymentAmount == 0 || ownerRent == 0 || expectedIncome == 0)
            return 0;
        var ownerPaid = paymentAmount * ownerRent / expectedIncome;
        if (ownerPaid < 0)
            return 0;
        return ownerPaid > ownerRent ? ownerRent : ownerPaid;
    }

    private static decimal CalculateUnpaidIncome(decimal invoicedIncome, decimal paidIncome) => Math.Max(0m, invoicedIncome - paidIncome);

    private static OwnerStartingBalance CalculateOwnerStartingBalance(IGrouping<string, JournalEntryLineSearchResult> group)
    {
        var orderedLines = group
            .OrderBy(line => line.TransactionDate)
            .ThenBy(line => line.JournalEntryCode)
            .ToList();
        var initialStartingBalanceLine = orderedLines
            .Where(line => IsOwnerStartingBalanceMemo(line.JournalEntryMemo, line.Memo))
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .FirstOrDefault();
        if (initialStartingBalanceLine == null)
        {
            return new OwnerStartingBalance
            {
                LedgerBalance = orderedLines.Sum(line => line.Credit - line.Debit)
            };
        }

        var openingAccountsPayableAmount = orderedLines
            .Where(line => line.JournalEntryId == initialStartingBalanceLine.JournalEntryId)
            .Sum(line => line.Credit - line.Debit);

        return new OwnerStartingBalance
        {
            LedgerBalance = orderedLines
                .Where(line => line.TransactionDate >= initialStartingBalanceLine.TransactionDate)
                .Sum(line => line.Credit - line.Debit),
            OpeningAccountsPayableAmount = openingAccountsPayableAmount,
            OpeningBalanceTransactionDate = initialStartingBalanceLine.TransactionDate
        };
    }

    private static decimal CalculateOwnerReportDrillDownAmount(JournalEntryRecapLine line, string metric, string category)
    {
        var amount = line.Amount;
        if (metric == "outstanding" && string.Equals(category, "OwnerPayment", StringComparison.OrdinalIgnoreCase))
            return -amount;

        if (metric == "balance" && string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase))
            return -amount;

        return amount;
    }
    #endregion

    #region Get

    private static List<int> GetReportOfficeIds(string officeIdsCsv)
    {
        return officeIdsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var officeId) ? officeId : 0)
            .Where(officeId => officeId > 0)
            .Distinct()
            .ToList();
    }

    private static OwnerStartingBalance GetOwnerStartingBalance(IReadOnlyDictionary<string, OwnerStartingBalance> startingBalanceByKey, int officeId, Guid propertyId)
    {
        if (startingBalanceByKey.TryGetValue(GetPropertyReportKey(officeId, propertyId), out var balance))
        {
            return new OwnerStartingBalance
            {
                OfficeId = officeId,
                PropertyId = propertyId,
                LedgerBalance = balance.LedgerBalance,
                OpeningAccountsPayableAmount = balance.OpeningAccountsPayableAmount,
                OpeningBalanceTransactionDate = balance.OpeningBalanceTransactionDate
            };
        }

        return new OwnerStartingBalance { OfficeId = officeId, PropertyId = propertyId };
    }

    private static string GetPropertyReportKey(int officeId, Guid propertyId)
        => $"{officeId}:{propertyId:D}";

    private static DateOnly? GetPriorMonthCloseDate(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate.HasValue)
            return startDate.Value.AddDays(-1);

        if (endDate.HasValue)
        {
            var firstDayOfMonth = new DateOnly(endDate.Value.Year, endDate.Value.Month, 1);
            return firstDayOfMonth.AddDays(-1);
        }

        return null;
    }

    private static DateOnly? GetReportPeriodStartDate(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate.HasValue)
            return startDate.Value;

        if (endDate.HasValue)
            return new DateOnly(endDate.Value.Year, endDate.Value.Month, 1);

        return null;
    }

    private static DateOnly? GetReportPeriodEndDate(DateOnly? startDate, DateOnly? endDate)
    {
        if (endDate.HasValue)
            return endDate.Value;

        if (startDate.HasValue)
            return startDate.Value;

        return null;
    }

    private static Dictionary<string, InvoiceOwnerIncomeTotals> GetInvoiceOwnerIncomeTotalsByInvoiceKey(IEnumerable<JournalEntryRecapLine> lines)
    {
        var totalsByKey = new Dictionary<string, InvoiceOwnerIncomeTotals>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines ?? [])
        {
            if (!TryGetRecapLinePropertyId(line, out _))
                continue;

            var category = (line.RecapCategory ?? string.Empty).Trim();
            if (!string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase) && !string.Equals(category, "ExpectedIncome", StringComparison.OrdinalIgnoreCase))
                continue;

            var invoiceKey = GetInvoiceOwnerIncomeKey(line);
            if (!totalsByKey.TryGetValue(invoiceKey, out var totals))
            {
                totals = new InvoiceOwnerIncomeTotals();
                totalsByKey[invoiceKey] = totals;
            }

            if (string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase))
            {
                totals.OwnerRentValue += line.Amount;
                var memo = (line.Description ?? string.Empty).Trim();
                var journalEntryCode = (line.JournalEntryCode ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(memo))
                    totals.OwnerRentMemo = memo;
                if (!string.IsNullOrWhiteSpace(journalEntryCode))
                    totals.OwnerRentJournalEntryCode = journalEntryCode;
                totals.OwnerRentAccountingPeriod = line.AccountingPeriod.ToString("yyyy-MM-dd");
                totals.OwnerRentJournalEntryLineId = line.JournalEntryLineId;
                totals.OwnerRentSourceId = line.SourceId;
                totals.OwnerRentSourceTypeId = line.SourceTypeId;
            }
            else
                totals.ExpectedIncomeValue += line.Amount;
        }

        return totalsByKey;
    }

    private static HashSet<Guid> GetPrepaymentPaymentSourceIds(IEnumerable<JournalEntryRecapLine> lines)
    {
        var sourceIds = new HashSet<Guid>();
        foreach (var line in lines ?? [])
        {
            if (!string.Equals(line.RecapCategory, "PrePayment", StringComparison.OrdinalIgnoreCase) || line.Amount <= 0 || !line.SourceId.HasValue || line.SourceId.Value == Guid.Empty)
                continue;

            sourceIds.Add(line.SourceId.Value);
        }

        return sourceIds;
    }

    private static Dictionary<string, DateOnly> GetInvoiceAccountingPeriodByInvoiceKey(IEnumerable<JournalEntryRecapLine> lines)
    {
        var periodByKey = new Dictionary<string, DateOnly>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines ?? [])
        {
            if (!TryGetRecapLinePropertyId(line, out var propertyId))
                continue;

            if (!string.Equals(line.RecapCategory, "OwnerRent", StringComparison.OrdinalIgnoreCase))
                continue;

            var invoiceKey = GetInvoiceOwnerIncomeKey(propertyId, GetRecapSourceDocumentCode(line));
            if (!periodByKey.TryGetValue(invoiceKey, out var existingPeriod) || line.AccountingPeriod < existingPeriod)
                periodByKey[invoiceKey] = line.AccountingPeriod;
        }

        return periodByKey;
    }

    private static string GetOwnerIncomeInvoiceSourceKey(JournalEntryRecapLine line, string category)
    {
        if (string.Equals(category, "Payment", StringComparison.OrdinalIgnoreCase) || (string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase) && line.Amount < 0))
        {
            foreach (var invoiceCode in GetRecapPaymentInvoiceSourceCodes(line))
                return invoiceCode;
        }

        var sourceDocumentCode = GetRecapSourceDocumentCode(line);
        if (!string.IsNullOrWhiteSpace(sourceDocumentCode))
            return sourceDocumentCode;

        if (line.SourceId.HasValue && line.SourceId.Value != Guid.Empty)
            return line.SourceId.Value.ToString("D");

        return line.JournalEntryLineId.ToString("D");
    }

    private static string GetInvoiceOwnerIncomeKey(JournalEntryRecapLine line)
    {
        if (!TryGetRecapLinePropertyId(line, out var propertyId))
            propertyId = Guid.Empty;

        return GetInvoiceOwnerIncomeKey(propertyId, GetRecapSourceDocumentCode(line));
    }

    private static string GetInvoiceOwnerIncomeKey(Guid propertyId, string sourceDocumentCode)
    {
        var sourceKey = (sourceDocumentCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sourceKey))
            sourceKey = "none";

        return $"{propertyId:D}|{sourceKey}";
    }

    private static string GetOwnerInvoiceActivityGroupKey(JournalEntryRecapLine line, string category)
    {
        if (!TryGetRecapLinePropertyId(line, out var propertyId))
            propertyId = Guid.Empty;

        var periodKey = line.AccountingPeriod.ToString("yyyy-MM-dd");
        if (string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase))
            return $"{propertyId:D}|{periodKey}|expense|{line.JournalEntryLineId:D}";

        var invoiceSourceKey = GetOwnerIncomeInvoiceSourceKey(line, category);
        return $"{propertyId:D}|income|{invoiceSourceKey}";
    }

    private static decimal GetOwnerPaidIncomeForInvoiceGroup(OwnerInvoiceActivityGroup group, string invoiceOwnerIncomeKey, IReadOnlyDictionary<string, InvoiceOwnerIncomeTotals> invoiceOwnerIncomeByKey)
    {
        invoiceOwnerIncomeByKey.TryGetValue(invoiceOwnerIncomeKey, out var invoiceOwnerIncome);
        var ownerRent = group.OwnerRentValue != 0 ? group.OwnerRentValue : invoiceOwnerIncome?.OwnerRentValue ?? 0;
        var expectedIncome = group.ExpectedIncomeValue != 0 ? group.ExpectedIncomeValue : invoiceOwnerIncome?.ExpectedIncomeValue ?? 0;
        return CalculateOwnerPaidIncome(ownerRent, expectedIncome, group.PaymentValue, group.OwnerPaymentReceivedValue);
    }

    private static int GetOwnerActivityLineSortOrder(OwnerStatementPropertyActivityLine line)
    {
        if (line.Expenses != 0 && line.ExpectedIncome == 0 && line.ReceivedIncome == 0 && line.PrepaidIncome == 0)
            return 3;
        if (line.ExpectedIncome > line.ReceivedIncome)
            return 0;
        if (line.PrepaidIncome != 0 && line.ExpectedIncome == 0 && line.ReceivedIncome == 0)
            return 2;
        if (line.ExpectedIncome == 0 && line.ReceivedIncome != 0)
            return 2;
        return 1;
    }

    private static string GetOwnerActivityAccountingPeriodSortKey(string? accountingPeriod)
    {
        if (string.IsNullOrWhiteSpace(accountingPeriod))
            return string.Empty;

        var trimmed = accountingPeriod.Trim();
        if (DateOnly.TryParseExact(trimmed, "MM.yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var monthYearPeriod))
            return monthYearPeriod.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        if (DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedPeriod))
            return parsedPeriod.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return trimmed;
    }

    private static string GetOwnerActivityRefNo(OwnerInvoiceActivityGroup group, InvoiceOwnerIncomeTotals? invoiceContext = null)
    {
        var refNo = (group.SourceDocumentCode ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(refNo))
            return refNo;

        return (group.InvoiceSourceCode ?? string.Empty).Trim();
    }

    private static string GetOwnerRentDocumentCode(OwnerInvoiceActivityGroup group, InvoiceOwnerIncomeTotals? invoiceContext = null)
    {
        var ownerRentJournalEntryCode = (group.OwnerRentJournalEntryCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(ownerRentJournalEntryCode))
            ownerRentJournalEntryCode = (invoiceContext?.OwnerRentJournalEntryCode ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(ownerRentJournalEntryCode))
            return ownerRentJournalEntryCode;

        return (group.SourceDocumentCode ?? string.Empty).Trim();
    }

    private static string GetOwnerRentActivityDescription(OwnerInvoiceActivityGroup group, InvoiceOwnerIncomeTotals? invoiceContext = null)
    {
        var memo = (group.OwnerRentMemo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(memo))
            memo = (invoiceContext?.OwnerRentMemo ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(memo))
            return StripOwnerMemoPrefixForDisplay(memo);

        return GetOwnerRentDocumentCode(group, invoiceContext);
    }

    private static string GetOwnerExpenseDocumentCode(OwnerInvoiceActivityGroup group)
    {
        var ownerExpenseJournalEntryCode = (group.OwnerExpenseJournalEntryCode ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(ownerExpenseJournalEntryCode))
            return ownerExpenseJournalEntryCode;

        return (group.SourceDocumentCode ?? string.Empty).Trim();
    }

    private static string GetOwnerExpenseActivityDescription(OwnerInvoiceActivityGroup group)
    {
        var memo = (group.OwnerExpenseMemo ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(memo))
            return StripOwnerMemoPrefixForDisplay(memo);

        return GetOwnerExpenseDocumentCode(group);
    }

    private static string GetTenantPaymentDocumentCode(OwnerInvoiceActivityGroup group)
    {
        var paymentJournalEntryCode = (group.PaymentJournalEntryCode ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(paymentJournalEntryCode))
            return paymentJournalEntryCode;

        var ownerPaymentJournalEntryCode = (group.OwnerPaymentJournalEntryCode ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(ownerPaymentJournalEntryCode))
            return ownerPaymentJournalEntryCode;

        return (group.SourceDocumentCode ?? string.Empty).Trim();
    }

    private static string GetTenantPaymentActivityDescription(OwnerInvoiceActivityGroup group)
    {
        var memo = (group.PaymentMemo ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(memo))
            return StripTenantPaymentMemoForDisplay(memo);

        memo = (group.OwnerPaymentMemo ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(memo))
            return StripOwnerMemoPrefixForDisplay(memo);

        return GetTenantPaymentDocumentCode(group);
    }

    #endregion

    #region Helpers

    private static bool IsOwnerStartingBalanceMemo(string? journalMemo, string? lineMemo)
    {
        var summaryMemo = (journalMemo ?? string.Empty).Trim();
        var detailMemo = (lineMemo ?? string.Empty).Trim();
        return summaryMemo.StartsWith(OwnerStartingBalanceMemoPrefix, StringComparison.OrdinalIgnoreCase)
            || detailMemo.StartsWith(OwnerStartingBalanceMemoPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRecapRowWithOwnerActivity(RecapReportRow row) =>
        row.OwnerRentValue != 0 || row.UnPaidValue != 0 || row.OwnerExpenseValue != 0 || row.OwnerPaymentReceivedValue != 0 || row.PaymentValue != 0;

    private static bool IsOwnerReportRecapCategory(string category) =>
        string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase)
        || string.Equals(category, "OwnerPayment", StringComparison.OrdinalIgnoreCase)
        || string.Equals(category, "ExpectedIncome", StringComparison.OrdinalIgnoreCase)
        || string.Equals(category, "Payment", StringComparison.OrdinalIgnoreCase)
        || string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase)
        || string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase);

    private static bool IsPrepaymentPaymentRecapLine(JournalEntryRecapLine line, IReadOnlySet<Guid> prepaymentPaymentSourceIds, IReadOnlyDictionary<string, DateOnly> invoiceAccountingPeriodByKey)
    {
        if (!string.Equals(line.RecapCategory, "Payment", StringComparison.OrdinalIgnoreCase))
            return false;

        if (line.SourceId.HasValue && line.SourceId.Value != Guid.Empty && prepaymentPaymentSourceIds.Contains(line.SourceId.Value))
            return true;

        if (!TryGetRecapLinePropertyId(line, out var propertyId))
            return false;

        var invoiceSourceCode = GetOwnerIncomeInvoiceSourceKey(line, "Payment");
        if (string.IsNullOrWhiteSpace(invoiceSourceCode))
            return false;

        var invoiceKey = GetInvoiceOwnerIncomeKey(propertyId, invoiceSourceCode);
        if (!invoiceAccountingPeriodByKey.TryGetValue(invoiceKey, out var invoiceAccountingPeriod))
            return false;

        return line.TransactionDate < invoiceAccountingPeriod;
    }

    private static bool IsOwnerInvoiceGroupWithActivity(OwnerInvoiceActivityGroup group) =>
        group.OwnerRentValue != 0 || group.OwnerExpenseValue != 0 || group.OwnerPaymentReceivedValue != 0 || group.PaymentValue != 0;

    private static bool TryGetRecapLinePropertyId(JournalEntryRecapLine line, out Guid propertyId)
    {
        if (line.PropertyId.HasValue && line.PropertyId.Value != Guid.Empty)
        {
            propertyId = line.PropertyId.Value;
            return true;
        }

        propertyId = Guid.Empty;
        return false;
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

    private static string StripOwnerMemoPrefixForDisplay(string memo)
    {
        var trimmed = (memo ?? string.Empty).Trim();
        if (trimmed.StartsWith("Owner:", StringComparison.OrdinalIgnoreCase))
            return trimmed["Owner:".Length..].TrimStart();

        return trimmed;
    }

    private static DateOnly ParseActivityDate(string transactionDate)
    {
        if (DateOnly.TryParse(transactionDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        return default;
    }

    #endregion

    #region Drilldown

    public async Task<IEnumerable<OwnerStatementJournalEntryLine>> GetOwnerReportJournalEntryLinesAsync(OwnerReportJournalEntryDrillDownCriteria criteria)
    {
        var officeIds = GetReportOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0 || criteria.OwnerId == Guid.Empty)
            return Enumerable.Empty<OwnerStatementJournalEntryLine>();

        var recapCriteria = new JournalEntryRecapGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        };

        var properties = await LoadOwnerPropertyReportDataAsync(recapCriteria);
        var propertyIdsForOwner = properties
            .Where(property => property.PrimaryOwnerId == criteria.OwnerId)
            .Where(property => !criteria.PropertyId.HasValue || property.PropertyId == criteria.PropertyId.Value)
            .Select(property => property.PropertyId)
            .ToHashSet();

        if (propertyIdsForOwner.Count == 0)
            return Enumerable.Empty<OwnerStatementJournalEntryLine>();

        var recapLines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(recapCriteria))
            .Where(line => line.PropertyId.HasValue && propertyIdsForOwner.Contains(line.PropertyId.Value))
            .Where(line => line.Amount != 0)
            .ToList();

        var metric = (criteria.Metric ?? string.Empty).Trim().ToLowerInvariant();
        return recapLines
            .Where(line => MatchesOwnerReportDrillDownMetric(line, metric))
            .Select(line => BuildOwnerReportJournalEntryLine(line, metric))
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .ThenByDescending(line => line.Amount)
            .ToList();
    }

    private static bool MatchesOwnerReportDrillDownMetric(JournalEntryRecapLine line, string metric)
    {
        var category = (line.RecapCategory ?? string.Empty).Trim();

        return metric switch
        {
            "expected" => string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase),
            "prepaid" => string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase),
            "paidincome" => string.Equals(category, "OwnerPayment", StringComparison.OrdinalIgnoreCase),
            "outstanding" => string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "OwnerPayment", StringComparison.OrdinalIgnoreCase),
            "income" => string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase),
            "expenses" => string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase),
            "balance" => string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static OwnerStatementJournalEntryLine BuildOwnerReportJournalEntryLine(JournalEntryRecapLine line, string metric)
    {
        var category = MapRecapCategoryToDrillDownCategory(line.RecapCategory);
        return new OwnerStatementJournalEntryLine
        {
            JournalEntryLineId = line.JournalEntryLineId,
            JournalEntryId = line.JournalEntryId,
            JournalEntryCode = line.JournalEntryCode,
            TransactionDate = line.TransactionDate,
            OfficeId = line.OfficeId,
            PropertyId = line.PropertyId ?? Guid.Empty,
            PropertyCode = (line.PropertyCode ?? string.Empty).Trim(),
            ChartOfAccountId = line.ChartOfAccountId,
            AccountNo = line.AccountNo,
            ChartOfAccountName = line.ChartOfAccountName,
            Description = line.Description,
            Debit = line.Debit,
            Credit = line.Credit,
            Category = category,
            Amount = CalculateOwnerReportDrillDownAmount(line, metric, category)
        };
    }

    private static string MapRecapCategoryToDrillDownCategory(string? recapCategory)
    {
        return (recapCategory ?? string.Empty).Trim() switch
        {
            "ExpectedIncome" => "Expected",
            "PrePayment" => "PrePaid",
            "Payment" => "PaidIncome",
            "OwnerRent" => "Actual",
            "OwnerPayment" => "OwnerPayment",
            "Expense" => "Expense",
            _ => (recapCategory ?? string.Empty).Trim()
        };
    }

    #endregion
}

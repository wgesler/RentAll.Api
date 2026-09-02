using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private OwnerCashReport BuildOwnerCashReport(OwnerReportLoadedData loaded, JournalEntryRecapGetCriteria criteria)
    {
        if (loaded.OfficeIds.Count == 0)
            return new OwnerCashReport();

        var recapLineSet = loaded.RecapLineSet;
        var lines = recapLineSet.AllLines;
        var activitySourceLines = GetOwnerCashActivitySourceLines(recapLineSet, criteria);
        LogOwnerReportCashActivitySourceLines(criteria, recapLineSet, activitySourceLines);

        var properties = loaded.Properties;
        var startingBalanceByKey = loaded.StartingBalanceByKey;
        var propertyActivityLines = FilterOwnerCashActivityLinesByAccountingPeriod(
            BuildOwnerActivityLines(activitySourceLines, lines, OwnerReportActivityMode.Cash), criteria);
        LogOwnerReportCashPropertyActivityLines(criteria, propertyActivityLines);

        var activityLinesByProperty = BuildOwnerActivityLinesByProperty(propertyActivityLines);
        var priorPeriodUnpaidByProperty = CalculatePriorPeriodUnpaidFromOutstanding(loaded.OutstandingInvoices, criteria);
        var ownerPaymentPaidByProperty = CalculateOwnerPaymentPaidByProperty(lines, criteria);

        var rows = properties
            .Select(property =>
            {
                var propertyKey = GetPropertyReportKey(property.OfficeId, property.PropertyId);
                var ownerStartingBalance = GetOwnerStartingBalance(startingBalanceByKey, property.OfficeId, property.PropertyId);
                priorPeriodUnpaidByProperty.TryGetValue(propertyKey, out var priorPeriodUnpaidIncome);

                var cancellableUnpaidIncome = Math.Min(
                    priorPeriodUnpaidIncome,
                    Math.Max(0m, ownerStartingBalance.LedgerBalance - ownerStartingBalance.OpeningAccountsPayableAmount));

                var startingBalance = ownerStartingBalance.LedgerBalance - cancellableUnpaidIncome;
                activityLinesByProperty.TryGetValue(propertyKey, out var activityLines);
                activityLines ??= [];

                var receivedIncome = activityLines.Sum(line => line.ReceivedIncome);
                var ownerExpenses = activityLines.Sum(line => line.Expenses);
                var ownerPayment = CalculateCashOwnerPayment(startingBalance, receivedIncome, ownerExpenses, property.WorkingCapitalBalance);
                var endingBalance = CalculateCashEndingBalance(startingBalance, receivedIncome, ownerExpenses, ownerPayment);
                ownerPaymentPaidByProperty.TryGetValue(propertyKey, out var ownerPaymentPaid);

                return new OwnerCashReportRow
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
                    ReceivedIncome = receivedIncome,
                    OwnerExpenses = ownerExpenses,
                    OwnerPayment = ownerPayment,
                    OwnerPaymentPaid = ownerPaymentPaid,
                    EndingBalance = endingBalance,
                    WorkingCapital = property.WorkingCapitalBalance
                };
            })
            .OrderBy(row => row.OfficeName)
            .ThenBy(row => row.PropertyCode)
            .ToList();

        return new OwnerCashReport
        {
            Rows = rows,
            PropertyActivityLines = propertyActivityLines
        };
    }

    #region Calculate

    private static Dictionary<string, decimal> CalculateOwnerPaymentPaidByProperty(IReadOnlyList<JournalEntryRecapLine> lines, JournalEntryRecapGetCriteria criteria)
    {
        return (lines ?? [])
            .Where(line => line.PropertyId.HasValue && line.PropertyId.Value != Guid.Empty)
            .Where(line => string.Equals(line.RecapCategory, "OwnerPayment", StringComparison.OrdinalIgnoreCase))
            .Where(line => IsAccountingPeriodInReportRange(line.AccountingPeriod, criteria.StartDate, criteria.EndDate))
            .GroupBy(line => GetPropertyReportKey(line.OfficeId, line.PropertyId!.Value))
            .ToDictionary(
                group => group.Key,
                group => group.Sum(line => Math.Abs(line.Amount)),
                StringComparer.OrdinalIgnoreCase);
    }

    private static decimal CalculateCashOwnerPayment(decimal startingBalance, decimal receivedIncome, decimal ownerExpenses, decimal workingCapitalBalance)
    {
        var ownerPayment = startingBalance + receivedIncome - ownerExpenses - workingCapitalBalance;
        return ownerPayment < 0 ? 0 : ownerPayment;
    }

    private static decimal CalculateCashEndingBalance(decimal startingBalance, decimal receivedIncome, decimal ownerExpenses, decimal ownerPayment)
    {
        var endingBalance = startingBalance + receivedIncome - ownerExpenses - ownerPayment;
        return endingBalance < 0 ? 0 : endingBalance;
    }

    private static Dictionary<string, decimal> CalculatePriorPeriodUnpaidFromOutstanding(IReadOnlyList<OwnerInvoiceOutstanding>? outstandingInvoices, JournalEntryRecapGetCriteria criteria)
    {
        var periodStart = GetReportPeriodStartDate(criteria.StartDate, criteria.EndDate);
        if (!periodStart.HasValue)
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        return (outstandingInvoices ?? [])
            .Where(row => row.AccountingPeriod < periodStart.Value)
            .GroupBy(row => GetPropertyReportKey(row.OfficeId, row.PropertyId), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(row => row.Outstanding),
                StringComparer.OrdinalIgnoreCase);
    }

    #endregion
}

using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private OwnerAccrualReport BuildOwnerAccrualReport(OwnerReportLoadedData loaded, JournalEntryRecapGetCriteria criteria)
    {
        if (loaded.OfficeIds.Count == 0)
            return new OwnerAccrualReport();

        var recapLineSet = loaded.RecapLineSet;
        var recapRows = BuildRecapReportRows(recapLineSet.ActivityLines);
        var properties = loaded.Properties;
        var startingBalanceByKey = loaded.StartingBalanceByKey;
        var recapRowsByProperty = recapRows
            .Where(row => row.PropertyId.HasValue && row.PropertyId.Value != Guid.Empty)
            .GroupBy(row => GetPropertyReportKey(row.OfficeId, row.PropertyId!.Value))
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var propertyActivityLines = BuildOwnerActivityLines(recapLineSet.ActivityLines, recapLineSet.AllLines, OwnerReportActivityMode.Accrual);
        var activityLinesByProperty = BuildOwnerActivityLinesByProperty(propertyActivityLines);

        var rows = properties
            .Select(property =>
            {
                var propertyKey = GetPropertyReportKey(property.OfficeId, property.PropertyId);
                var startingBalance = GetOwnerStartingBalance(startingBalanceByKey, property.OfficeId, property.PropertyId).LedgerBalance;
                activityLinesByProperty.TryGetValue(propertyKey, out var activityLines);
                activityLines ??= [];
                recapRowsByProperty.TryGetValue(propertyKey, out var propertyRecapRows);
                propertyRecapRows ??= [];

                var invoicedIncome = activityLines.Sum(line => line.ExpectedIncome);
                var paidIncome = activityLines.Sum(line => line.ReceivedIncome);
                var prepaidIncome = CalculateAccrualPrepaidIncome(propertyRecapRows);
                var ownerExpenses = activityLines.Sum(line => line.Expenses);
                var unpaidIncome = CalculateUnpaidIncome(invoicedIncome, paidIncome);
                var ownerProfit = CalculateAccrualOwnerProfit(activityLines);

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

    #region Calculate

    private static decimal CalculateAccrualOwnerProfit(IEnumerable<OwnerStatementPropertyActivityLine> activityLines) =>
        (activityLines ?? []).Sum(line => line.ReceivedIncome - line.Expenses);

    private static decimal CalculateAccrualPrepaidIncome(IReadOnlyList<RecapReportRow> propertyRecapRows)
    {
        return propertyRecapRows
            .Where(row => IsRecapRowWithOwnerActivity(row))
            .GroupBy(row => row.ReservationCode ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(row => row.AccountingPeriod, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.SortDateValue).Last().PrePaymentValue)
            .Sum();
    }

    #endregion
}

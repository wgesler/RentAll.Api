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
        var properties = loaded.Properties;
        var startingBalanceByKey = loaded.StartingBalanceByKey;
        var propertyActivityLines = BuildOwnerActivityLines(recapLineSet.ActivityLines, recapLineSet.AllLines, OwnerReportActivityMode.Accrual);
        var activityLinesByProperty = BuildOwnerActivityLinesByProperty(propertyActivityLines);

        var rows = properties
            .Select(property =>
            {
                var propertyKey = GetPropertyReportKey(property.OfficeId, property.PropertyId);
                var startingBalance = GetOwnerStartingBalance(startingBalanceByKey, property.OfficeId, property.PropertyId).LedgerBalance;
                activityLinesByProperty.TryGetValue(propertyKey, out var activityLines);
                activityLines ??= [];

                var invoicedIncome = activityLines.Sum(line => line.ExpectedIncome);
                var ownerExpenses = activityLines.Sum(line => line.Expenses);
                var ownerProfit = CalculateAccrualOwnerProfit(invoicedIncome, ownerExpenses);

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

    private static decimal CalculateAccrualOwnerProfit(decimal invoicedIncome, decimal ownerExpenses) =>
        Math.Max(0m, invoicedIncome - ownerExpenses);

    #endregion
}

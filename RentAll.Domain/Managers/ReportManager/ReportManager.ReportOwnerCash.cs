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

        var activitySourceLines = recapLineSet.ActivityLines;

        var properties = loaded.Properties;

        var startingBalanceByKey = loaded.StartingBalanceByKey;

        var propertyActivityLines = BuildOwnerActivityLines(activitySourceLines, lines, OwnerReportActivityMode.Cash);

        var activityLinesByProperty = BuildOwnerActivityLinesByProperty(propertyActivityLines);

        var priorPeriodUnpaidByProperty = CalculatePriorPeriodUnpaidByProperty(lines, criteria, startingBalanceByKey);

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

    private static Dictionary<string, decimal> CalculatePriorPeriodUnpaidByProperty(IReadOnlyList<JournalEntryRecapLine> lines, JournalEntryRecapGetCriteria criteria, IReadOnlyDictionary<string, OwnerStartingBalance> startingBalanceByKey)

    {

        var periodStart = GetReportPeriodStartDate(criteria.StartDate, criteria.EndDate);

        if (!periodStart.HasValue)

            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var priorPeriodUnpaidByProperty = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var propertyGroup in (lines ?? [])

                     .Where(line => line.PropertyId.HasValue && line.PropertyId.Value != Guid.Empty)

                     .Where(line => line.TransactionDate < periodStart.Value)

                     .GroupBy(line => GetPropertyReportKey(line.OfficeId, line.PropertyId!.Value)))

        {

            var anchorLine = propertyGroup.First();

            var openingBalanceTransactionDate = GetOwnerStartingBalance(startingBalanceByKey, anchorLine.OfficeId, anchorLine.PropertyId!.Value).OpeningBalanceTransactionDate;

            var priorPeriodSourceLines = propertyGroup.AsEnumerable();

            if (openingBalanceTransactionDate.HasValue)

            {

                priorPeriodSourceLines = priorPeriodSourceLines

                    .Where(line => line.TransactionDate >= openingBalanceTransactionDate.Value);

            }

            var priorPeriodLines = priorPeriodSourceLines.ToList();

            if (priorPeriodLines.Count == 0)

                continue;

            var priorPeriodActivityLines = BuildOwnerActivityLines(priorPeriodLines, lines, OwnerReportActivityMode.Accrual);

            var invoicedIncome = priorPeriodActivityLines.Sum(line => line.ExpectedIncome);

            var paidIncome = priorPeriodActivityLines.Sum(line => line.ReceivedIncome);

            priorPeriodUnpaidByProperty[propertyGroup.Key] = CalculateUnpaidIncome(invoicedIncome, paidIncome);

        }

        return priorPeriodUnpaidByProperty;

    }

    #endregion

}

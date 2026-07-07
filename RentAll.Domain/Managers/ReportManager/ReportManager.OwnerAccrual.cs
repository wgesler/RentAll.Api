using System.Globalization;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    public async Task<OwnerAccrualReport> GetOwnerAccrualReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        var lines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(criteria)).ToList();
        var recapRows = BuildRecapReportRows(lines);
        var officeIds = ParseReportOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
            return new OwnerAccrualReport();

        var properties = await LoadOwnerCashPropertyReportDataAsync(criteria);
        var propertyActivityLines = BuildOwnerAccrualPropertyActivityLines(recapRows);
        var activityLinesByProperty = BuildOwnerReportPropertyActivityLinesByKey(propertyActivityLines);

        var rows = properties
            .Select(property =>
            {
                var propertyKey = BuildOwnerCashPropertyKey(property.OfficeId, property.PropertyId);
                activityLinesByProperty.TryGetValue(propertyKey, out var activityLines);
                activityLines ??= [];

                var invoicedIncome = activityLines.Sum(line => line.ExpectedIncome);
                var paidIncome = activityLines.Sum(line => line.ReceivedIncome);
                var prepaidIncome = activityLines.Sum(line => line.OwnerPayment);
                var ownerExpenses = activityLines.Sum(line => line.Expenses);
                var unpaidIncome = activityLines.Sum(line => line.ExpectedIncome - line.ReceivedIncome);
                var ownerProfit = activityLines.Sum(line => line.ReceivedIncome - line.Expenses);

                return new OwnerAccrualReportRow
                {
                    PropertyId = property.PropertyId,
                    OfficeId = property.OfficeId,
                    OfficeName = property.OfficeName,
                    OwnerId = property.PrimaryOwnerId,
                    PropertyCode = property.PropertyCode,
                    OwnerName = (property.OwnerNames ?? string.Empty).Trim(),
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

    private static List<OwnerStatementPropertyActivityLine> BuildOwnerAccrualPropertyActivityLines(IEnumerable<RecapReportRow> recapRows)
    {
        return recapRows
            .Where(row => row.PropertyId.HasValue && row.PropertyId.Value != Guid.Empty)
            .Where(row => HasOwnerAccrualReportRecapActivity(row))
            .Select(row => new OwnerStatementPropertyActivityLine
            {
                PropertyId = row.PropertyId!.Value,
                OfficeId = row.OfficeId,
                ActivityId = ResolveOwnerCashActivityJournalEntryLineId(row),
                SourceId = row.SourceId,
                JournalEntryLineId = ResolveOwnerCashActivityJournalEntryLineId(row),
                ActivityType = row.ActivityType,
                ActivityDate = ParseOwnerAccrualActivityDate(row.TransactionDate),
                AccountingPeriod = (row.AccountingPeriod ?? string.Empty).Trim(),
                DocumentCode = ResolveOwnerCashActivityDocumentCode(row),
                Description = ResolveOwnerCashActivityDescription(row),
                ExpectedIncome = row.ExpectedIncomeValue,
                ReceivedIncome = row.PaymentValue,
                Expenses = row.OwnerExpenseValue,
                OwnerPayment = row.PrePaymentValue
            })
            .OrderBy(line => line.OfficeId)
            .ThenBy(line => line.PropertyId)
            .ThenBy(line => line.ActivityDate)
            .ThenBy(line => line.DocumentCode)
            .ToList();
    }

    private static DateOnly ParseOwnerAccrualActivityDate(string transactionDate)
    {
        if (DateOnly.TryParse(transactionDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        return default;
    }
}

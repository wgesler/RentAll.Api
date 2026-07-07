using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    public async Task<TransferReport> GetTransferReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        var historyCriteria = new JournalEntryRecapGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            ReservationId = criteria.ReservationId,
            StartDate = null,
            EndDate = null,
            IncludeVoided = false,
            IncludeUnposted = true,
            RecapCategory = criteria.RecapCategory
        };

        var lines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(historyCriteria)).ToList();
        var recapRows = BuildRecapReportRows(lines)
            .Where(row => !row.IsPosted)
            .OrderBy(row => row.SortDateValue)
            .ThenBy(row => row.PropertyCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.ReservationCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.AccountingPeriod, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.JournalEntryCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var runningTotalByRowKey = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var runningTotal = 0m;
        foreach (var row in recapRows)
        {
            var businessValue = row.RentPlus4000Value - row.OwnerRentValue;
            runningTotal += businessValue;
            runningTotalByRowKey[BuildTransferReportRowKey(row)] = runningTotal;
        }

        var filteredRows = recapRows
            .Where(row => IsTransferReportRowInDateRange(row, criteria.StartDate, criteria.EndDate))
            .Select(row => MapTransferReportRow(row, runningTotalByRowKey))
            .ToList();

        return new TransferReport
        {
            Rows = filteredRows
        };
    }

    private static string BuildTransferReportRowKey(RecapReportRow row) =>
        $"{row.JournalEntryId}:{row.JournalEntryLineId}:{row.SortDateValue}";

    private static bool IsTransferReportRowInDateRange(RecapReportRow row, DateOnly? startDate, DateOnly? endDate)
    {
        if (!startDate.HasValue && !endDate.HasValue)
            return true;

        if (row.SortDateValue <= 0)
            return !startDate.HasValue && !endDate.HasValue;

        var transactionDate = new DateTime(row.SortDateValue).Date;
        if (startDate.HasValue && transactionDate < startDate.Value.ToDateTime(TimeOnly.MinValue).Date)
            return false;

        if (endDate.HasValue && transactionDate > endDate.Value.ToDateTime(TimeOnly.MinValue).Date)
            return false;

        return true;
    }

    private static TransferReportRow MapTransferReportRow(
        RecapReportRow row,
        IReadOnlyDictionary<string, decimal> runningTotalByRowKey)
    {
        var businessValue = row.RentPlus4000Value - row.OwnerRentValue;
        runningTotalByRowKey.TryGetValue(BuildTransferReportRowKey(row), out var runningTotalUnpostedValue);

        return new TransferReportRow
        {
            PropertyCode = row.PropertyCode,
            ReservationCode = row.ReservationCode,
            AccountingPeriod = row.AccountingPeriod,
            Source = row.Source,
            JournalEntryCode = row.JournalEntryCode,
            SourceTypeId = row.SourceTypeId,
            SourceId = row.SourceId,
            SourceLinkable = row.SourceLinkable,
            ActivityType = row.ActivityType,
            OfficeId = row.OfficeId,
            PropertyId = row.PropertyId,
            ReservationId = row.ReservationId,
            TransactionDate = row.TransactionDate,
            ExpectedIncome = row.ExpectedIncome,
            RentPlus4000 = row.RentPlus4000,
            OwnerRent = row.OwnerRent,
            Business = FormatCurrencyUsd(businessValue),
            SecurityDeposit = row.SecurityDeposit,
            Sdw = row.Sdw,
            Fee = row.Fee,
            RunningTotalUnposted = FormatCurrencyUsd(runningTotalUnpostedValue),
            ExpectedIncomeValue = row.ExpectedIncomeValue,
            RentPlus4000Value = row.RentPlus4000Value,
            OwnerRentValue = row.OwnerRentValue,
            BusinessValue = businessValue,
            SecurityDepositValue = row.SecurityDepositValue,
            SdwValue = row.SdwValue,
            FeeValue = row.FeeValue,
            RunningTotalUnpostedValue = runningTotalUnpostedValue,
            SortDateValue = row.SortDateValue,
            JournalEntryId = row.JournalEntryId,
            JournalEntryLineId = row.JournalEntryLineId
        };
    }
}

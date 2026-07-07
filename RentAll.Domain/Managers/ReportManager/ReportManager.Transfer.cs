using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    public async Task<TransferReport> GetTransferReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        var recapCriteria = new JournalEntryRecapGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            ReservationId = criteria.ReservationId,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate,
            IncludeVoided = false,
            IncludeUnposted = true,
            RecapCategory = criteria.RecapCategory
        };

        var lines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(recapCriteria)).ToList();
        var recapRows = BuildRecapReportRows(lines)
            .Where(row => !row.IsPosted)
            .ToList();

        var rows = ConsolidateTransferReportRowsBySource(recapRows);

        return new TransferReport
        {
            Rows = rows
        };
    }

    private static List<TransferReportRow> ConsolidateTransferReportRowsBySource(IEnumerable<RecapReportRow> recapRows)
    {
        var groups = new Dictionary<string, List<RecapReportRow>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in recapRows)
        {
            var key = BuildTransferReportSourceKey(row);
            if (!groups.TryGetValue(key, out var groupRows))
            {
                groupRows = [];
                groups[key] = groupRows;
            }

            groupRows.Add(row);
        }

        return groups.Values
            .Select(MapConsolidatedTransferReportRow)
            .Where(HasTransferReportMeaningfulAmount)
            .OrderBy(row => row.PropertyCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.ReservationCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.SortDateValue)
            .ThenBy(row => row.Source, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildTransferReportSourceKey(RecapReportRow row)
    {
        var source = (row.Source ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(source))
            return source.ToUpperInvariant();

        var sourceId = row.SourceId?.ToString() ?? string.Empty;
        return $"{row.OfficeId}|{row.SourceTypeId}|{sourceId}".ToUpperInvariant();
    }

    private static TransferReportRow MapConsolidatedTransferReportRow(IReadOnlyList<RecapReportRow> rows)
    {
        var orderedRows = rows
            .OrderBy(row => row.SortDateValue)
            .ThenBy(row => row.AccountingPeriod, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.JournalEntryCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var primaryRow = orderedRows[0];

        var expectedIncomeValue = orderedRows.Sum(row => row.ExpectedIncomeValue);
        var rentPlus4000Value = orderedRows.Sum(row => row.RentPlus4000Value);
        var ownerRentValue = orderedRows.Sum(row => row.OwnerRentValue);
        var securityDepositValue = orderedRows.Sum(row => row.SecurityDepositValue);
        var sdwValue = orderedRows.Sum(row => row.SdwValue);
        var feeValue = orderedRows.Sum(row => row.FeeValue);
        var businessValue = rentPlus4000Value - ownerRentValue;

        return new TransferReportRow
        {
            PropertyCode = primaryRow.PropertyCode,
            ReservationCode = primaryRow.ReservationCode,
            AccountingPeriod = primaryRow.AccountingPeriod,
            Source = primaryRow.Source,
            JournalEntryCode = primaryRow.JournalEntryCode,
            SourceTypeId = primaryRow.SourceTypeId,
            SourceId = primaryRow.SourceId,
            SourceLinkable = primaryRow.SourceLinkable,
            ActivityType = primaryRow.ActivityType,
            OfficeId = primaryRow.OfficeId,
            PropertyId = primaryRow.PropertyId,
            ReservationId = primaryRow.ReservationId,
            TransactionDate = primaryRow.TransactionDate,
            ExpectedIncome = FormatCurrencyUsd(expectedIncomeValue),
            RentPlus4000 = FormatCurrencyUsd(rentPlus4000Value),
            OwnerRent = FormatCurrencyUsd(ownerRentValue),
            Business = FormatCurrencyUsd(businessValue),
            SecurityDeposit = FormatCurrencyUsd(securityDepositValue),
            Sdw = FormatCurrencyUsd(sdwValue),
            Fee = FormatCurrencyUsd(feeValue),
            ExpectedIncomeValue = expectedIncomeValue,
            RentPlus4000Value = rentPlus4000Value,
            OwnerRentValue = ownerRentValue,
            BusinessValue = businessValue,
            SecurityDepositValue = securityDepositValue,
            SdwValue = sdwValue,
            FeeValue = feeValue,
            SortDateValue = primaryRow.SortDateValue,
            JournalEntryId = primaryRow.JournalEntryId,
            JournalEntryLineId = primaryRow.JournalEntryLineId
        };
    }

    private static bool HasTransferReportMeaningfulAmount(TransferReportRow row) =>
        row.ExpectedIncomeValue != 0
        || row.RentPlus4000Value != 0
        || row.OwnerRentValue != 0
        || row.BusinessValue != 0
        || row.SecurityDepositValue != 0
        || row.SdwValue != 0
        || row.FeeValue != 0;
}

using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    public async Task<IEnumerable<OwnerStatementJournalEntryLine>> GetEscrowReportJournalEntryLinesAsync(
        EscrowReportJournalEntryDrillDownCriteria criteria)
    {
        var officeIds = GetReportOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0 || !criteria.EndDate.HasValue)
            return [];

        var recapCriteria = BuildEscrowDrillDownRecapCriteria(criteria);
        var metric = (criteria.Metric ?? string.Empty).Trim().ToLowerInvariant();

        return metric switch
        {
            "arbalance" => BuildEscrowArBalanceLines(await LoadEscrowDrillDownRecapLinesAsync(recapCriteria)),
            "notcollected" => BuildEscrowNotCollectedLines(await LoadEscrowDrillDownRecapLinesAsync(recapCriteria)),
            "prepaids" => await BuildEscrowPrepaidLinesAsync(recapCriteria),
            "total" => await BuildEscrowTotalLinesAsync(recapCriteria),
            "e2" => await BuildEscrowE2LinesAsync(recapCriteria),
            "escrowbankbalance" => await BuildEscrowBankBalanceLinesAsync(recapCriteria),
            "transfer" => await BuildEscrowTransferLinesAsync(recapCriteria),
            _ => []
        };
    }

    private static JournalEntryRecapGetCriteria BuildEscrowDrillDownRecapCriteria(
        EscrowReportJournalEntryDrillDownCriteria criteria)
    {
        return new JournalEntryRecapGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            StartDate = null,
            EndDate = criteria.EndDate,
            IncludeUnposted = criteria.IncludeUnposted,
            IncludePaymentInvoiceContext = true
        };
    }

    private async Task<List<JournalEntryRecapLine>> LoadEscrowDrillDownRecapLinesAsync(JournalEntryRecapGetCriteria criteria)
    {
        var lines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(criteria)).ToList();
        return FilterEscrowDrillDownRecapLines(lines, criteria.PropertyId);
    }

    private static List<JournalEntryRecapLine> FilterEscrowDrillDownRecapLines(
        IEnumerable<JournalEntryRecapLine> lines,
        Guid? propertyId)
    {
        if (!propertyId.HasValue || propertyId.Value == Guid.Empty)
            return lines.Where(line => line.Amount != 0).ToList();

        return lines
            .Where(line => line.PropertyId.HasValue && line.PropertyId.Value == propertyId.Value)
            .Where(line => line.Amount != 0)
            .ToList();
    }

    private static IEnumerable<OwnerStatementJournalEntryLine> BuildEscrowArBalanceLines(IEnumerable<JournalEntryRecapLine> recapLines)
        => recapLines
            .Where(line => string.Equals(line.RecapCategory, "OwnerRent", StringComparison.OrdinalIgnoreCase))
            .Select(line => BuildEscrowJournalEntryLine(line, "Actual", line.Amount))
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .ThenByDescending(line => line.Amount);

    private static IEnumerable<OwnerStatementJournalEntryLine> BuildEscrowNotCollectedLines(IEnumerable<JournalEntryRecapLine> recapLines)
    {
        var lines = recapLines.ToList();
        var unpaidInvoiceKeys = lines
            .Where(line => line.PropertyId.HasValue)
            .GroupBy(line => $"{line.PropertyId!.Value:N}|{(line.SourceDocumentCode ?? string.Empty).Trim()}", StringComparer.OrdinalIgnoreCase)
            .Where(group => CalculateEscrowInvoiceUnpaidAmount(group) > 0.005m)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return lines
            .Where(line => line.PropertyId.HasValue
                && unpaidInvoiceKeys.Contains($"{line.PropertyId.Value:N}|{(line.SourceDocumentCode ?? string.Empty).Trim()}"))
            .Where(line => string.Equals(line.RecapCategory, "OwnerRent", StringComparison.OrdinalIgnoreCase))
            .Select(line => BuildEscrowJournalEntryLine(line, "Actual", line.Amount))
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .ThenByDescending(line => line.Amount);
    }

    private static decimal CalculateEscrowInvoiceUnpaidAmount(IEnumerable<JournalEntryRecapLine> invoiceLines)
    {
        var ownerRent = invoiceLines
            .Where(line => string.Equals(line.RecapCategory, "OwnerRent", StringComparison.OrdinalIgnoreCase))
            .Sum(line => line.Amount);
        var ownerPaid = invoiceLines
            .Where(line => string.Equals(line.RecapCategory, "OwnerRentActual", StringComparison.OrdinalIgnoreCase))
            .Sum(line => line.Amount);

        return CalculateUnpaidIncome(ownerRent, ownerPaid);
    }

    private async Task<IEnumerable<OwnerStatementJournalEntryLine>> BuildEscrowPrepaidLinesAsync(
        JournalEntryRecapGetCriteria criteria)
    {
        var rows = await _journalEntryRepository.GetEscrowPrepaidApplyJournalEntryLinesAsync(criteria);
        return rows
            .Where(row => row.Amount != 0)
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .ThenByDescending(line => line.Amount);
    }

    private async Task<IEnumerable<OwnerStatementJournalEntryLine>> BuildEscrowTotalLinesAsync(
        JournalEntryRecapGetCriteria criteria)
    {
        var recapLines = await LoadEscrowDrillDownRecapLinesAsync(criteria);
        return DistinctEscrowJournalEntryLines(
            BuildEscrowArBalanceLines(recapLines)
                .Concat(BuildEscrowNotCollectedLines(recapLines))
                .Concat(await BuildEscrowPrepaidLinesAsync(criteria)));
    }

    private async Task<IEnumerable<OwnerStatementJournalEntryLine>> BuildEscrowE2LinesAsync(
        JournalEntryRecapGetCriteria criteria)
    {
        if (criteria.PropertyId.HasValue && criteria.PropertyId.Value != Guid.Empty)
        {
            var recapLines = await LoadEscrowDrillDownRecapLinesAsync(criteria);
            var arBalance = recapLines
                .Where(line => string.Equals(line.RecapCategory, "OwnerRent", StringComparison.OrdinalIgnoreCase))
                .Sum(line => line.Amount);
            var prepaids = (await _journalEntryRepository.GetEscrowPrepaidApplyJournalEntryLinesAsync(criteria))
                .Sum(line => line.Amount);
            var notCollected = recapLines
                .Where(line => line.PropertyId.HasValue)
                .GroupBy(line => $"{line.PropertyId!.Value:N}|{(line.SourceDocumentCode ?? string.Empty).Trim()}", StringComparer.OrdinalIgnoreCase)
                .Sum(group => CalculateEscrowInvoiceUnpaidAmount(group));
            var total = arBalance - prepaids - notCollected;
            if (total <= 0.005m)
                return [];

            return await BuildEscrowTotalLinesAsync(criteria);
        }

        var properties = await LoadOwnerPropertyReportDataAsync(criteria);
        var propertyIds = properties
            .Where(property => !criteria.PropertyId.HasValue || property.PropertyId == criteria.PropertyId.Value)
            .Select(property => property.PropertyId)
            .ToHashSet();

        var allLines = new List<OwnerStatementJournalEntryLine>();
        foreach (var propertyId in propertyIds)
        {
            var propertyCriteria = new JournalEntryRecapGetCriteria
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = criteria.OfficeIds,
                PropertyId = propertyId,
                StartDate = null,
                EndDate = criteria.EndDate,
                IncludeUnposted = criteria.IncludeUnposted,
                IncludePaymentInvoiceContext = true
            };

            var propertyLines = await BuildEscrowE2LinesAsync(propertyCriteria);
            allLines.AddRange(propertyLines);
        }

        return DistinctEscrowJournalEntryLines(allLines);
    }

    private async Task<IEnumerable<OwnerStatementJournalEntryLine>> BuildEscrowBankBalanceLinesAsync(
        JournalEntryRecapGetCriteria criteria)
    {
        var rows = await _journalEntryRepository.GetEscrowBankJournalEntryLinesAsync(criteria);
        return rows
            .Where(row => row.Amount != 0)
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .ThenByDescending(line => Math.Abs(line.Amount));
    }

    private async Task<IEnumerable<OwnerStatementJournalEntryLine>> BuildEscrowTransferLinesAsync(
        JournalEntryRecapGetCriteria criteria)
    {
        var propertyCriteria = new JournalEntryRecapGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = null,
            StartDate = null,
            EndDate = criteria.EndDate,
            IncludeUnposted = criteria.IncludeUnposted,
            IncludePaymentInvoiceContext = true
        };

        return DistinctEscrowJournalEntryLines(
            (await BuildEscrowTotalLinesAsync(propertyCriteria))
                .Concat(await BuildEscrowBankBalanceLinesAsync(criteria)));
    }

    private static IEnumerable<OwnerStatementJournalEntryLine> DistinctEscrowJournalEntryLines(
        IEnumerable<OwnerStatementJournalEntryLine> lines)
        => lines
            .GroupBy(line => line.JournalEntryLineId)
            .Select(group => group.First())
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .ThenByDescending(line => line.Amount);

    private static OwnerStatementJournalEntryLine BuildEscrowJournalEntryLine(
        JournalEntryRecapLine line,
        string category,
        decimal amount)
    {
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
            Amount = amount
        };
    }

}

using Microsoft.Extensions.Logging;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private void LogOwnerReportBundleRecap(JournalEntryRecapGetCriteria criteria, RecapLineSet recapLineSet)
    {
        var allLines = recapLineSet.AllLines ?? [];
        var ownerRentActualLines = allLines
            .Where(line => string.Equals(line.RecapCategory, "OwnerRentActual", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var ownerRentLines = allLines
            .Where(line => string.Equals(line.RecapCategory, "OwnerRent", StringComparison.OrdinalIgnoreCase))
            .ToList();

        _logger.LogError(
            "[OwnerReportTrace] Recap classified: AllLines={AllLines} ActivityLines={ActivityLines} OwnerRent={OwnerRentCount} OwnerRentActual={OwnerRentActualCount} StartDate={StartDate} EndDate={EndDate} PropertyId={PropertyId}",
            allLines.Count,
            recapLineSet.ActivityLines?.Count ?? 0,
            ownerRentLines.Count,
            ownerRentActualLines.Count,
            criteria.StartDate,
            criteria.EndDate,
            criteria.PropertyId);

        foreach (var line in ownerRentActualLines)
        {
            _logger.LogError(
                "[OwnerReportTrace] OwnerRentActual recap line: JE={JournalEntryCode} LineId={JournalEntryLineId} TxnDate={TransactionDate} AcctPeriod={AccountingPeriod} IsInDateRange={IsInDateRange} SourceDoc={SourceDocumentCode} Property={PropertyCode} Amount={Amount} Desc={Description}",
                line.JournalEntryCode,
                line.JournalEntryLineId,
                line.TransactionDate,
                line.AccountingPeriod,
                line.IsInDateRange,
                line.SourceDocumentCode,
                line.PropertyCode,
                line.Amount,
                line.Description);
        }
    }

    private void LogOwnerReportCashActivitySourceLines(JournalEntryRecapGetCriteria criteria, RecapLineSet recapLineSet, IReadOnlyList<JournalEntryRecapLine> activitySourceLines)
    {
        var supplementalOwnerRentActual = activitySourceLines
            .Where(line => string.Equals(line.RecapCategory, "OwnerRentActual", StringComparison.OrdinalIgnoreCase))
            .Where(line => !line.IsInDateRange)
            .ToList();

        _logger.LogError(
            "[OwnerReportTrace] Cash activity source lines: Total={Total} OwnerRentActual={OwnerRentActualCount} SupplementalOutOfRangeTxnOwnerRentActual={SupplementalCount}",
            activitySourceLines.Count,
            activitySourceLines.Count(line => string.Equals(line.RecapCategory, "OwnerRentActual", StringComparison.OrdinalIgnoreCase)),
            supplementalOwnerRentActual.Count);

        foreach (var line in supplementalOwnerRentActual)
        {
            _logger.LogError(
                "[OwnerReportTrace] Supplemental OwnerRentActual source line: JE={JournalEntryCode} LineId={JournalEntryLineId} TxnDate={TransactionDate} AcctPeriod={AccountingPeriod} SourceDoc={SourceDocumentCode} Property={PropertyCode} Amount={Amount}",
                line.JournalEntryCode,
                line.JournalEntryLineId,
                line.TransactionDate,
                line.AccountingPeriod,
                line.SourceDocumentCode,
                line.PropertyCode,
                line.Amount);
        }
    }

    private void LogOwnerReportCashPropertyActivityLines(JournalEntryRecapGetCriteria criteria, IEnumerable<OwnerStatementPropertyActivityLine> propertyActivityLines)
    {
        var lines = propertyActivityLines?.ToList() ?? [];
        var incomeLines = lines
            .Where(line => line.ReceivedIncome != 0 || line.ExpectedIncome != 0)
            .ToList();

        _logger.LogError(
            "[OwnerReportTrace] Cash property activity lines: Total={Total} IncomeRelated={IncomeRelatedCount} StartDate={StartDate} EndDate={EndDate} PropertyId={PropertyId}",
            lines.Count,
            incomeLines.Count,
            criteria.StartDate,
            criteria.EndDate,
            criteria.PropertyId);

        foreach (var line in incomeLines)
        {
            _logger.LogError(
                "[OwnerReportTrace] Cash activity income line: Property={PropertyId} SourceDoc={SourceDocumentCode} DocCode={DocumentCode} AcctPeriod={AccountingPeriod} ActivityDate={ActivityDate} Expected={ExpectedIncome} Received={ReceivedIncome} Desc={Description}",
                line.PropertyId,
                line.SourceDocumentCode,
                line.DocumentCode,
                line.AccountingPeriod,
                line.ActivityDate,
                line.ExpectedIncome,
                line.ReceivedIncome,
                line.Description);
        }
    }
}

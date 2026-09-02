using Microsoft.Extensions.Logging;
using RentAll.Domain.Accounting;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class JournalEntryRepository
{
    private void LogOwnerReportRecapRawLines(string source, JournalEntryRecapGetCriteria criteria, IReadOnlyList<JournalEntryRecapRawLineEntity> rawLines, bool includeOwnerReportSupplemental)
    {
        var ownerActualRawLines = rawLines
            .Where(line => line.JournalEntryKindId == (int)JournalEntryKind.OwnerActual)
            .ToList();

        _logger.LogError(
            "[OwnerReportTrace] Recap raw from SQL ({Source}): TotalRaw={TotalRaw} OwnerActualRaw={OwnerActualRaw} IncludeOwnerReportSupplemental={IncludeOwnerReportSupplemental} IncludePaymentInvoiceContext={IncludePaymentInvoiceContext} StartDate={StartDate} EndDate={EndDate} PropertyId={PropertyId}",
            source,
            rawLines.Count,
            ownerActualRawLines.Count,
            includeOwnerReportSupplemental,
            criteria.IncludePaymentInvoiceContext,
            criteria.StartDate,
            criteria.EndDate,
            criteria.PropertyId);

        foreach (var line in ownerActualRawLines)
        {
            _logger.LogError(
                "[OwnerReportTrace] OwnerActual raw from SQL: JE={JournalEntryCode} LineId={JournalEntryLineId} TxnDate={TransactionDate} AcctPeriod={AccountingPeriod} IsInDateRange={IsInDateRange} SourceDoc={SourceDocumentCode} Property={PropertyCode} ChartAcct={ChartOfAccountId} OwnApAcct={DefaultOwnActPayableAccountId} Debit={Debit} Credit={Credit}",
                line.JournalEntryCode,
                line.JournalEntryLineId,
                line.TransactionDate,
                line.AccountingPeriod,
                line.IsInDateRange,
                line.SourceDocumentCode,
                line.PropertyCode,
                line.ChartOfAccountId,
                line.DefaultOwnActPayableAccountId,
                line.Debit,
                line.Credit);
        }
    }

    private void LogOwnerActualRawDrop(JournalEntryRecapRawLineEntity rawLine, string reason, JournalEntryRecapGetCriteria criteria, JournalEntryRecapClassificationLine classificationLine, string? recapCategory = null)
    {
        _logger.LogError(
            "[OwnerReportTrace] OwnerActual raw dropped ({Reason}): JE={JournalEntryCode} LineId={JournalEntryLineId} TxnDate={TransactionDate} AcctPeriod={AccountingPeriod} IsInDateRange={IsInDateRange} SourceDoc={SourceDocumentCode} Property={PropertyCode} ChartAcct={ChartOfAccountId} OwnApAcct={DefaultOwnActPayableAccountId} Debit={Debit} Credit={Credit} RecapCategory={RecapCategory} IncludePaymentInvoiceContext={IncludePaymentInvoiceContext}",
            reason,
            rawLine.JournalEntryCode,
            rawLine.JournalEntryLineId,
            rawLine.TransactionDate,
            rawLine.AccountingPeriod,
            rawLine.IsInDateRange,
            rawLine.SourceDocumentCode,
            rawLine.PropertyCode,
            rawLine.ChartOfAccountId,
            classificationLine.DefaultOwnActPayableAccountId,
            rawLine.Debit,
            rawLine.Credit,
            recapCategory,
            criteria.IncludePaymentInvoiceContext);
    }
}

using Microsoft.Data.SqlClient;
using RentAll.Domain.Accounting;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class JournalEntryRepository
{
    public async Task<IEnumerable<JournalEntryRecapLine>> GetJournalEntryRecapLinesAsync(JournalEntryRecapGetCriteria criteria)
    {
        var rawLines = (await QueryJournalEntryRecapRawLinesAsync(criteria)).ToList();
        if (rawLines.Count == 0)
            return Enumerable.Empty<JournalEntryRecapLine>();

        return ClassifyAndFilterRecapLines(rawLines, criteria);
    }

    public async Task<OwnerReportBundleData> GetOwnerReportBundleDataAsync(
        JournalEntryRecapGetCriteria criteria,
        DateOnly? priorMonthCloseDate,
        DateOnly? periodStartDate)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (recapRaw, ownerApRaw) = await db.DapperProcQueryMultipleAsync<
            JournalEntryRecapRawLineEntity,
            JournalEntryLineSearchEntity>(
            "Accounting.JournalEntryRecap_GetByCriteria",
            BuildJournalEntryRecapProcParameters(
                criteria,
                includeOwnerReportSupplemental: true,
                includeEscrowSupplemental: false,
                priorMonthCloseDate,
                periodStartDate),
            commandTimeout: 120);

        return new OwnerReportBundleData
        {
            RecapLines = ClassifyAndFilterRecapLines(recapRaw ?? [], criteria).ToList(),
            OwnerApLines = (ownerApRaw ?? []).Select(ConvertLineSearchEntityToModel).ToList()
        };
    }

    public async Task<EscrowReportBundleData> GetEscrowReportDataAsync(JournalEntryRecapGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (recapRaw, escrowRaw, prepaidRaw) = await db.DapperProcQueryTripleAsync<
            JournalEntryRecapRawLineEntity,
            EscrowOfficeBalanceEntity,
            EscrowPrepaidPropertyBalanceEntity>(
            "Accounting.JournalEntryRecap_GetByCriteria",
            BuildJournalEntryRecapProcParameters(
                criteria,
                includeOwnerReportSupplemental: false,
                includeEscrowSupplemental: true,
                priorMonthCloseDate: null,
                periodStartDate: null),
            commandTimeout: 120);

        return new EscrowReportBundleData
        {
            RecapLines = ClassifyAndFilterRecapLines(recapRaw ?? [], criteria).ToList(),
            EscrowOfficeBalances = (escrowRaw ?? []).Select(ConvertEscrowOfficeBalanceEntityToModel).ToList(),
            EscrowPrepaidPropertyBalances = (prepaidRaw ?? []).Select(ConvertEscrowPrepaidPropertyBalanceEntityToModel).ToList()
        };
    }

    private async Task<IEnumerable<JournalEntryRecapRawLineEntity>> QueryJournalEntryRecapRawLinesAsync(
        JournalEntryRecapGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<JournalEntryRecapRawLineEntity>(
            "Accounting.JournalEntryRecap_GetByCriteria",
            BuildJournalEntryRecapProcParameters(
                criteria,
                includeOwnerReportSupplemental: false,
                includeEscrowSupplemental: false,
                priorMonthCloseDate: null,
                periodStartDate: null),
            commandTimeout: 120);

        return res ?? Enumerable.Empty<JournalEntryRecapRawLineEntity>();
    }

    private static object BuildJournalEntryRecapProcParameters(
        JournalEntryRecapGetCriteria criteria,
        bool includeOwnerReportSupplemental,
        bool includeEscrowSupplemental,
        DateOnly? priorMonthCloseDate,
        DateOnly? periodStartDate)
    {
        return new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            ReservationId = criteria.ReservationId,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate,
            IncludeVoided = criteria.IncludeVoided,
            IncludeUnposted = criteria.IncludeUnposted,
            IncludePaymentInvoiceContext = criteria.IncludePaymentInvoiceContext,
            ReachBackInvoiceCodes = (string?)null,
            IncludeOwnerReportSupplemental = includeOwnerReportSupplemental,
            IncludeEscrowSupplemental = includeEscrowSupplemental,
            PriorMonthCloseDate = priorMonthCloseDate,
            PeriodStartDate = periodStartDate
        };
    }

    private static EscrowOfficeBalance ConvertEscrowOfficeBalanceEntityToModel(EscrowOfficeBalanceEntity entity)
    {
        return new EscrowOfficeBalance
        {
            OfficeId = entity.OfficeId,
            AccountId = entity.AccountId,
            AccountNo = entity.AccountNo,
            AccountName = entity.AccountName,
            Balance = entity.Balance
        };
    }

    private static EscrowPrepaidPropertyBalance ConvertEscrowPrepaidPropertyBalanceEntityToModel(EscrowPrepaidPropertyBalanceEntity entity)
    {
        return new EscrowPrepaidPropertyBalance
        {
            OfficeId = entity.OfficeId,
            PropertyId = entity.PropertyId,
            Balance = entity.Balance
        };
    }

    private static IEnumerable<JournalEntryRecapLine> ClassifyAndFilterRecapLines(
        IEnumerable<JournalEntryRecapRawLineEntity> rawLines,
        JournalEntryRecapGetCriteria criteria)
    {
        var recapCategoryFilter = (criteria.RecapCategory ?? string.Empty).Trim();
        var hasRecapCategoryFilter = !string.IsNullOrWhiteSpace(recapCategoryFilter);

        foreach (var rawLine in rawLines)
        {
            var classificationLine = ConvertRawEntityToClassificationLine(rawLine);
            if (!JournalEntryRecapLineClassifier.TryClassify(classificationLine, out var classification))
                continue;

            if (classification.Amount == 0)
                continue;

            if (hasRecapCategoryFilter
                && !string.Equals(classification.RecapCategory, recapCategoryFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!rawLine.IsInDateRange)
            {
                if (!criteria.IncludePaymentInvoiceContext)
                    continue;

                var isReachBackCategory = string.Equals(classification.RecapCategory, "OwnerRent", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(classification.RecapCategory, "OwnerRentActual", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(classification.RecapCategory, "ExpectedIncome", StringComparison.OrdinalIgnoreCase);
                if (!isReachBackCategory)
                    continue;
            }

            yield return ConvertClassifiedRawEntityToModel(rawLine, classification);
        }
    }

    private static JournalEntryRecapClassificationLine ConvertRawEntityToClassificationLine(JournalEntryRecapRawLineEntity rawLine)
    {
        return new JournalEntryRecapClassificationLine
        {
            SourceTypeId = rawLine.SourceTypeId,
            JournalEntryKindId = rawLine.JournalEntryKindId,
            SourceDocumentCode = rawLine.SourceDocumentCode,
            ChartOfAccountId = rawLine.ChartOfAccountId,
            Debit = rawLine.Debit,
            Credit = rawLine.Credit,
            LineMemo = rawLine.LineMemo,
            JournalEntryMemo = rawLine.JournalEntryMemo,
            DefaultActRcvableAccountId = rawLine.DefaultActRcvableAccountId,
            DefaultUndepFundsAccountId = rawLine.DefaultUndepFundsAccountId,
            DefaultPrePayAccountId = rawLine.DefaultPrePayAccountId,
            DefaultOwnActPayableAccountId = rawLine.DefaultOwnActPayableAccountId,
            DefaultOwnerExpAccountId = rawLine.DefaultOwnerExpAccountId,
            DefaultTenantIncAccountId = rawLine.DefaultTenantIncAccountId,
            IsRentalIncomeAccount = rawLine.IsRentalIncomeAccount,
            IsCashOnly = rawLine.IsCashOnly,
            IsInDateRange = rawLine.IsInDateRange
        };
    }

    private static JournalEntryRecapLine ConvertClassifiedRawEntityToModel(
        JournalEntryRecapRawLineEntity rawLine,
        JournalEntryRecapClassificationResult classification)
    {
        return new JournalEntryRecapLine
        {
            JournalEntryLineId = rawLine.JournalEntryLineId,
            JournalEntryId = rawLine.JournalEntryId,
            JournalEntryCode = rawLine.JournalEntryCode,
            TransactionDate = rawLine.TransactionDate,
            AccountingPeriod = rawLine.AccountingPeriod,
            OfficeId = rawLine.OfficeId,
            PropertyId = rawLine.PropertyId,
            PropertyCode = rawLine.PropertyCode,
            ReservationId = rawLine.ReservationId,
            ReservationCode = rawLine.ReservationCode,
            SourceTypeId = rawLine.SourceTypeId,
            SourceId = rawLine.SourceId,
            PostingStatusId = rawLine.PostingStatusId,
            SourceTypeCode = rawLine.SourceTypeCode,
            SourceDocumentCode = rawLine.SourceDocumentCode,
            ChartOfAccountId = rawLine.ChartOfAccountId,
            AccountNo = rawLine.AccountNo,
            ChartOfAccountName = rawLine.ChartOfAccountName,
            Description = rawLine.Description,
            Debit = rawLine.Debit,
            Credit = rawLine.Credit,
            Activity = classification.Activity,
            RecapCategory = classification.RecapCategory,
            Amount = classification.Amount,
            IsInDateRange = rawLine.IsInDateRange
        };
    }
}

using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class JournalEntryRepository
{
    public async Task<IEnumerable<JournalEntryRecapLine>> GetJournalEntryRecapLinesAsync(JournalEntryRecapGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<JournalEntryRecapLineEntity>("Accounting.JournalEntryRecap_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            ReservationId = criteria.ReservationId,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate,
            IncludeVoided = criteria.IncludeVoided,
            IncludeUnposted = criteria.IncludeUnposted,
            RecapCategory = string.IsNullOrWhiteSpace(criteria.RecapCategory) ? null : criteria.RecapCategory.Trim()
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<JournalEntryRecapLine>();

        return res.Select(ConvertRecapLineEntityToModel);
    }

    private static JournalEntryRecapLine ConvertRecapLineEntityToModel(JournalEntryRecapLineEntity e)
    {
        return new JournalEntryRecapLine
        {
            JournalEntryLineId = e.JournalEntryLineId,
            JournalEntryId = e.JournalEntryId,
            JournalEntryCode = e.JournalEntryCode,
            TransactionDate = e.TransactionDate,
            AccountingPeriod = e.AccountingPeriod,
            OfficeId = e.OfficeId,
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            ReservationId = e.ReservationId,
            ReservationCode = e.ReservationCode,
            SourceTypeId = e.SourceTypeId,
            SourceId = e.SourceId,
            SourceTypeCode = e.SourceTypeCode,
            SourceDocumentCode = e.SourceDocumentCode,
            ChartOfAccountId = e.ChartOfAccountId,
            AccountNo = e.AccountNo,
            ChartOfAccountName = e.ChartOfAccountName,
            Description = e.Description,
            Debit = e.Debit,
            Credit = e.Credit,
            Activity = e.Activity,
            RecapCategory = e.RecapCategory,
            Amount = e.Amount
        };
    }
}

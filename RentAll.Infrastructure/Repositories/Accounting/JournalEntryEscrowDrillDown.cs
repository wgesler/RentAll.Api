using Dapper;
using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class JournalEntryRepository
{
    public async Task<IEnumerable<OwnerStatementJournalEntryLine>> GetEscrowPrepaidApplyJournalEntryLinesAsync(
        JournalEntryRecapGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        const string sql = """
            SELECT
                jel.[JournalEntryLineId],
                je.[JournalEntryId],
                je.[JournalEntryCode],
                je.[TransactionDate],
                je.[OfficeId],
                PropertyId = COALESCE(
                    NULLIF(jel.[PropertyId], '00000000-0000-0000-0000-000000000000'),
                    r.[PropertyId],
                    propertyFromReservation.[PropertyId]
                ),
                PropertyCode = COALESCE(
                    p.[PropertyCode],
                    propertyFromReservation.[PropertyCode],
                    ''
                ),
                jel.[ChartOfAccountId],
                AccountNo = coa.[AccountNo],
                ChartOfAccountName = coa.[Name],
                Description = COALESCE(NULLIF(LTRIM(RTRIM(jel.[Memo])), ''), NULLIF(LTRIM(RTRIM(je.[Memo])), ''), ''),
                jel.[Debit],
                jel.[Credit],
                Amount = jel.[Debit],
                Category = 'PrePaid'
            FROM [Accounting].[JournalEntry] AS je
            INNER JOIN (
                SELECT CAST(value AS INT) AS OfficeId
                FROM STRING_SPLIT(@OfficeIds, ',')
                WHERE RTRIM(LTRIM(value)) <> ''
            ) AS oi
                ON je.[OfficeId] = oi.[OfficeId]
            INNER JOIN [Organization].[AccountingOffice] AS ao
                ON ao.[OrganizationId] = @OrganizationId
                AND ao.[OfficeId] = je.[OfficeId]
            INNER JOIN [Accounting].[JournalEntryLine] AS jel
                ON jel.[JournalEntryId] = je.[JournalEntryId]
                AND jel.[ChartOfAccountId] = ao.[DefaultPrePayAccountId]
                AND jel.[Debit] > 0
            INNER JOIN [Accounting].[Invoice] AS sourceInvoice
                ON sourceInvoice.[OrganizationId] = @OrganizationId
                AND je.[SourceTypeId] IN (2, 3, 4)
                AND je.[SourceId] = sourceInvoice.[InvoiceId]
            LEFT OUTER JOIN [Property].[Reservation] AS r
                ON jel.[ReservationId] = r.[ReservationId]
            LEFT OUTER JOIN [Property].[Property] AS p
                ON jel.[PropertyId] = p.[PropertyId]
            LEFT OUTER JOIN [Property].[Property] AS propertyFromReservation
                ON r.[PropertyId] = propertyFromReservation.[PropertyId]
            LEFT OUTER JOIN [Accounting].[ChartOfAccounts] AS coa
                ON coa.[OrganizationId] = @OrganizationId
                AND coa.[OfficeId] = je.[OfficeId]
                AND coa.[AccountId] = jel.[ChartOfAccountId]
            WHERE
                je.[OrganizationId] = @OrganizationId
                AND (@IncludeUnposted = 1 OR je.[PostingStatusId] <> 0)
                AND ao.[DefaultPrePayAccountId] IS NOT NULL
                AND je.[JournalEntryKindId] = 5
                AND (@EndDate IS NULL OR sourceInvoice.[AccountingPeriod] >= @EndDate)
                AND (@PropertyId IS NULL OR COALESCE(
                    NULLIF(jel.[PropertyId], '00000000-0000-0000-0000-000000000000'),
                    r.[PropertyId],
                    propertyFromReservation.[PropertyId]
                ) = @PropertyId)
                AND (@ReservationId IS NULL OR jel.[ReservationId] = @ReservationId)
            """;

        var rows = await db.QueryAsync<EscrowJournalEntryLineEntity>(sql, new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            ReservationId = criteria.ReservationId,
            EndDate = criteria.EndDate,
            IncludeUnposted = criteria.IncludeUnposted
        });

        return (rows ?? []).Select(ConvertEscrowJournalEntryLineEntityToModel);
    }

    public async Task<IEnumerable<OwnerStatementJournalEntryLine>> GetEscrowBankJournalEntryLinesAsync(
        JournalEntryRecapGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        const string sql = """
            SELECT
                jel.[JournalEntryLineId],
                je.[JournalEntryId],
                je.[JournalEntryCode],
                je.[TransactionDate],
                je.[OfficeId],
                PropertyId = COALESCE(NULLIF(jel.[PropertyId], '00000000-0000-0000-0000-000000000000'), NULL),
                PropertyCode = COALESCE(p.[PropertyCode], ''),
                jel.[ChartOfAccountId],
                AccountNo = coa.[AccountNo],
                ChartOfAccountName = coa.[Name],
                Description = COALESCE(NULLIF(LTRIM(RTRIM(jel.[Memo])), ''), NULLIF(LTRIM(RTRIM(je.[Memo])), ''), ''),
                jel.[Debit],
                jel.[Credit],
                Amount = jel.[Credit] - jel.[Debit],
                Category = 'EscrowBank'
            FROM [Accounting].[JournalEntry] AS je
            INNER JOIN (
                SELECT CAST(value AS INT) AS OfficeId
                FROM STRING_SPLIT(@OfficeIds, ',')
                WHERE RTRIM(LTRIM(value)) <> ''
            ) AS oi
                ON je.[OfficeId] = oi.[OfficeId]
            INNER JOIN [Organization].[AccountingOffice] AS ao
                ON ao.[OrganizationId] = @OrganizationId
                AND ao.[OfficeId] = je.[OfficeId]
            INNER JOIN [Accounting].[JournalEntryLine] AS jel
                ON jel.[JournalEntryId] = je.[JournalEntryId]
                AND jel.[ChartOfAccountId] = ao.[DefaultEscrowOwnersAccountId]
            LEFT OUTER JOIN [Property].[Property] AS p
                ON jel.[PropertyId] = p.[PropertyId]
            LEFT OUTER JOIN [Accounting].[ChartOfAccounts] AS coa
                ON coa.[OrganizationId] = @OrganizationId
                AND coa.[OfficeId] = je.[OfficeId]
                AND coa.[AccountId] = jel.[ChartOfAccountId]
            WHERE
                je.[OrganizationId] = @OrganizationId
                AND (@IncludeUnposted = 1 OR je.[PostingStatusId] <> 0)
                AND ao.[DefaultEscrowOwnersAccountId] IS NOT NULL
                AND (@EndDate IS NULL OR je.[TransactionDate] <= @EndDate)
            """;

        var rows = await db.QueryAsync<EscrowJournalEntryLineEntity>(sql, new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            EndDate = criteria.EndDate,
            IncludeUnposted = criteria.IncludeUnposted
        });

        return (rows ?? []).Select(ConvertEscrowJournalEntryLineEntityToModel);
    }

    private static OwnerStatementJournalEntryLine ConvertEscrowJournalEntryLineEntityToModel(
        EscrowJournalEntryLineEntity entity)
    {
        return new OwnerStatementJournalEntryLine
        {
            JournalEntryLineId = entity.JournalEntryLineId,
            JournalEntryId = entity.JournalEntryId,
            JournalEntryCode = entity.JournalEntryCode,
            TransactionDate = entity.TransactionDate,
            OfficeId = entity.OfficeId,
            PropertyId = entity.PropertyId ?? Guid.Empty,
            PropertyCode = (entity.PropertyCode ?? string.Empty).Trim(),
            ChartOfAccountId = entity.ChartOfAccountId,
            AccountNo = entity.AccountNo,
            ChartOfAccountName = entity.ChartOfAccountName,
            Description = entity.Description,
            Debit = entity.Debit,
            Credit = entity.Credit,
            Category = string.IsNullOrWhiteSpace(entity.Category) ? "Other" : entity.Category.Trim(),
            Amount = entity.Amount
        };
    }
}

using Dapper;
using Microsoft.Data.SqlClient;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class JournalEntryRepository
{
    public async Task<EscrowReportBundleData> GetEscrowReportDataAsync(JournalEntryRecapGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (propertyRaw, prepaidRaw, notCollectedRaw) = await db.DapperProcQueryTripleAsync<
            EscrowPropertyReportDataEntity,
            EscrowPrepaidPropertyBalanceEntity,
            EscrowNotCollectedPropertyBalanceEntity>(
            "Accounting.JournalEntryLine_GetOwnerEscrowByCriteria",
            new
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = criteria.OfficeIds,
                PropertyId = criteria.PropertyId,
                ReservationId = criteria.ReservationId,
                EndDate = criteria.EndDate,
                IncludeUnposted = criteria.IncludeUnposted
            },
            commandTimeout: 120);

        var escrowOfficeBalances = (await QueryEscrowOfficeBalancesAsync(db, criteria)).ToList();

        return new EscrowReportBundleData
        {
            Properties = (propertyRaw ?? []).Select(ConvertEscrowPropertyReportDataEntityToModel).ToList(),
            PrepaidPropertyBalances = (prepaidRaw ?? []).Select(ConvertEscrowPrepaidPropertyBalanceEntityToModel).ToList(),
            NotCollectedPropertyBalances = (notCollectedRaw ?? []).Select(ConvertEscrowNotCollectedPropertyBalanceEntityToModel).ToList(),
            EscrowOfficeBalances = escrowOfficeBalances
        };
    }

    private async Task<IEnumerable<EscrowOfficeBalance>> QueryEscrowOfficeBalancesAsync(
        SqlConnection db,
        JournalEntryRecapGetCriteria criteria)
    {
        const string sql = """
            SELECT
                ao.[OfficeId],
                AccountId = ao.[DefaultEscrowOwnersAccountId],
                AccountNo = coa.[AccountNo],
                AccountName = coa.[Name],
                Balance = ISNULL(SUM(jel.[Credit] - jel.[Debit]), 0)
            FROM (
                SELECT CAST(value AS INT) AS OfficeId
                FROM STRING_SPLIT(@OfficeIds, ',')
                WHERE RTRIM(LTRIM(value)) <> ''
            ) AS oi
            INNER JOIN [Organization].[AccountingOffice] AS ao
                ON ao.[OrganizationId] = @OrganizationId
                AND ao.[OfficeId] = oi.[OfficeId]
            LEFT OUTER JOIN [Accounting].[ChartOfAccounts] AS coa
                ON coa.[OrganizationId] = @OrganizationId
                AND coa.[OfficeId] = ao.[OfficeId]
                AND coa.[AccountId] = ao.[DefaultEscrowOwnersAccountId]
            LEFT OUTER JOIN [Accounting].[JournalEntry] AS je
                ON je.[OrganizationId] = @OrganizationId
                AND je.[OfficeId] = ao.[OfficeId]
                AND (@IncludeUnposted = 1 OR je.[PostingStatusId] <> 0)
                AND (@EndDate IS NULL OR je.[TransactionDate] <= @EndDate)
            LEFT OUTER JOIN [Accounting].[JournalEntryLine] AS jel
                ON jel.[JournalEntryId] = je.[JournalEntryId]
                AND jel.[ChartOfAccountId] = ao.[DefaultEscrowOwnersAccountId]
            WHERE
                ao.[DefaultEscrowOwnersAccountId] IS NOT NULL
            GROUP BY
                ao.[OfficeId],
                ao.[DefaultEscrowOwnersAccountId],
                coa.[AccountNo],
                coa.[Name]
            ORDER BY
                ao.[OfficeId]
            """;

        var rows = await db.QueryAsync<EscrowOfficeBalanceEntity>(sql, new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            EndDate = criteria.EndDate,
            IncludeUnposted = criteria.IncludeUnposted
        });

        return (rows ?? []).Select(ConvertEscrowOfficeBalanceEntityToModel);
    }

    private static EscrowPropertyReportData ConvertEscrowPropertyReportDataEntityToModel(EscrowPropertyReportDataEntity entity)
    {
        return new EscrowPropertyReportData
        {
            PropertyId = entity.PropertyId,
            PropertyCode = entity.PropertyCode,
            OfficeId = entity.OfficeId,
            OfficeName = entity.OfficeName,
            PropertyType = (PropertyType)entity.PropertyTypeId,
            PropertyTypeDescription = entity.PropertyType,
            PropertyLeaseType = (PropertyLeaseType)entity.PropertyLeaseTypeId,
            PrimaryOwnerId = entity.PrimaryOwnerId,
            OwnerType = entity.OwnerTypeId.HasValue ? (OwnerType)entity.OwnerTypeId.Value : null,
            CompanyName = entity.CompanyName,
            OwnerNames = entity.OwnerNames,
            OwnerNameLine = entity.OwnerNameLine,
            WorkingCapitalBalance = entity.WorkingCapitalBalance,
            ManagementFeeType = (ManagementFeeType)entity.ManagementFeeTypeId,
            RevenueSplitOwner = entity.RevenueSplitOwner,
            RevenueSplitOffice = entity.RevenueSplitOffice,
            ApBalance = entity.ApBalance
        };
    }

    private static EscrowNotCollectedPropertyBalance ConvertEscrowNotCollectedPropertyBalanceEntityToModel(
        EscrowNotCollectedPropertyBalanceEntity entity)
    {
        return new EscrowNotCollectedPropertyBalance
        {
            OfficeId = entity.OfficeId,
            PropertyId = entity.PropertyId,
            NotCollectedAmount = entity.NotCollectedAmount
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

    private static EscrowPrepaidPropertyBalance ConvertEscrowPrepaidPropertyBalanceEntityToModel(
        EscrowPrepaidPropertyBalanceEntity entity)
    {
        return new EscrowPrepaidPropertyBalance
        {
            OfficeId = entity.OfficeId,
            PropertyId = entity.PropertyId,
            Balance = entity.Balance
        };
    }
}

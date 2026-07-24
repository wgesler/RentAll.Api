using Microsoft.Data.SqlClient;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class JournalEntryRepository
{
    private const string EscrowReportProcName = "Accounting.JournalEntryLine_GetOwnerEscrowByCriteria";

    public async Task<EscrowReportBundleData> GetEscrowReportBundleDataAsync(
        JournalEntryRecapGetCriteria criteria,
        bool includeDrillDownLines = false)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var procParameters = BuildEscrowReportProcParameters(criteria, includeDrillDownLines);

        if (!includeDrillDownLines)
        {
            var (propertyRaw, prepaidRaw, notCollectedRaw, escrowOfficeRaw) = await db.DapperProcQueryQuadrupleAsync<
                EscrowPropertyReportDataEntity,
                EscrowPrepaidPropertyBalanceEntity,
                EscrowNotCollectedPropertyBalanceEntity,
                EscrowOfficeBalanceEntity>(
                EscrowReportProcName,
                procParameters,
                commandTimeout: 120);

            return BuildEscrowReportBundleData(propertyRaw, prepaidRaw, notCollectedRaw, escrowOfficeRaw);
        }

        var (
            propertyWithDrillDownRaw,
            prepaidWithDrillDownRaw,
            notCollectedWithDrillDownRaw,
            escrowOfficeWithDrillDownRaw,
            ownerApRaw,
            prepaidApplyRaw,
            escrowBankRaw) = await db.DapperProcQuerySeptupleAsync<
            EscrowPropertyReportDataEntity,
            EscrowPrepaidPropertyBalanceEntity,
            EscrowNotCollectedPropertyBalanceEntity,
            EscrowOfficeBalanceEntity,
            EscrowJournalEntryLineEntity,
            EscrowJournalEntryLineEntity,
            EscrowJournalEntryLineEntity>(
            EscrowReportProcName,
            procParameters,
            commandTimeout: 120);

        var bundle = BuildEscrowReportBundleData(
            propertyWithDrillDownRaw,
            prepaidWithDrillDownRaw,
            notCollectedWithDrillDownRaw,
            escrowOfficeWithDrillDownRaw);
        bundle.OwnerApLines = (ownerApRaw ?? []).Select(ConvertEscrowJournalEntryLineEntityToModel).ToList();
        bundle.PrepaidApplyLines = (prepaidApplyRaw ?? []).Select(ConvertEscrowJournalEntryLineEntityToModel).ToList();
        bundle.EscrowBankLines = (escrowBankRaw ?? []).Select(ConvertEscrowJournalEntryLineEntityToModel).ToList();
        return bundle;
    }

    private static object BuildEscrowReportProcParameters(
        JournalEntryRecapGetCriteria criteria,
        bool includeDrillDownLines)
    {
        return new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            ReservationId = criteria.ReservationId,
            EndDate = criteria.EndDate,
            IncludeUnposted = criteria.IncludeUnposted,
            IncludeDrillDownLines = includeDrillDownLines
        };
    }

    private static EscrowReportBundleData BuildEscrowReportBundleData(
        IEnumerable<EscrowPropertyReportDataEntity>? propertyRaw,
        IEnumerable<EscrowPrepaidPropertyBalanceEntity>? prepaidRaw,
        IEnumerable<EscrowNotCollectedPropertyBalanceEntity>? notCollectedRaw,
        IEnumerable<EscrowOfficeBalanceEntity>? escrowOfficeRaw)
    {
        return new EscrowReportBundleData
        {
            Properties = (propertyRaw ?? []).Select(ConvertEscrowPropertyReportDataEntityToModel).ToList(),
            PrepaidPropertyBalances = (prepaidRaw ?? []).Select(ConvertEscrowPrepaidPropertyBalanceEntityToModel).ToList(),
            NotCollectedPropertyBalances = (notCollectedRaw ?? []).Select(ConvertEscrowNotCollectedPropertyBalanceEntityToModel).ToList(),
            EscrowOfficeBalances = (escrowOfficeRaw ?? []).Select(ConvertEscrowOfficeBalanceEntityToModel).ToList()
        };
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

using Microsoft.Data.SqlClient;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

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
            var (propertyRaw, escrowOfficeRaw) = await db.DapperProcQueryMultipleAsync<
                EscrowPropertyReportDataEntity,
                EscrowOfficeBalanceEntity>(
                EscrowReportProcName,
                procParameters,
                commandTimeout: 120);

            return BuildEscrowReportBundleData(propertyRaw, escrowOfficeRaw);
        }

        var (
            propertyWithDrillDownRaw,
            escrowOfficeWithDrillDownRaw,
            ownerApRaw,
            prepaidApplyRaw,
            escrowBankRaw) = await db.DapperProcQueryQuintupleAsync<
            EscrowPropertyReportDataEntity,
            EscrowOfficeBalanceEntity,
            EscrowJournalEntryLineEntity,
            EscrowJournalEntryLineEntity,
            EscrowJournalEntryLineEntity>(
            EscrowReportProcName,
            procParameters,
            commandTimeout: 120);

        var bundle = BuildEscrowReportBundleData(propertyWithDrillDownRaw, escrowOfficeWithDrillDownRaw);
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
        IEnumerable<EscrowOfficeBalanceEntity>? escrowOfficeRaw)
    {
        var properties = (propertyRaw ?? []).Select(ConvertEscrowPropertyReportDataEntityToModel).ToList();

        return new EscrowReportBundleData
        {
            Properties = properties,
            PrepaidPropertyBalances = properties
                .Where(property => property.Prepaids > 0.005m)
                .Select(property => new EscrowPrepaidPropertyBalance
                {
                    PropertyId = property.PropertyId,
                    PropertyCode = property.PropertyCode,
                    OfficeId = property.OfficeId,
                    Prepaids = property.Prepaids
                })
                .ToList(),
            NotCollectedPropertyBalances = properties
                .Where(property =>
                    Math.Abs(property.ExpectedIncome) > 0.005m
                    || Math.Abs(property.ActualIncome) > 0.005m
                    || Math.Abs(property.NotCollectedAmount) > 0.005m)
                .Select(property => new EscrowNotCollectedPropertyBalance
                {
                    PropertyId = property.PropertyId,
                    PropertyCode = property.PropertyCode,
                    OfficeId = property.OfficeId,
                    ExpectedIncome = property.ExpectedIncome,
                    ActualIncome = property.ActualIncome,
                    NotCollectedAmount = property.NotCollectedAmount
                })
                .ToList(),
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
            ApBalance = entity.ApBalance,
            Prepaids = entity.Prepaids,
            ExpectedIncome = entity.ExpectedIncome,
            ActualIncome = entity.ActualIncome,
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

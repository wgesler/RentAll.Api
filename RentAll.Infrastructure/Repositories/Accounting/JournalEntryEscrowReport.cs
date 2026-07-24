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
        var (propertyRaw, prepaidRaw, notCollectedRaw, escrowOfficeRaw) = await db.DapperProcQueryQuadrupleAsync<
            EscrowPropertyReportDataEntity,
            EscrowPrepaidPropertyBalanceEntity,
            EscrowNotCollectedPropertyBalanceEntity,
            EscrowOfficeBalanceEntity>(
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
}

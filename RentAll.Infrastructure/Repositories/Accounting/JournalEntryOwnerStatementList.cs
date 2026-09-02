using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class JournalEntryRepository
{
    private const string OwnerStatementListProcName = "Accounting.OwnerStatementList_GetByCriteria";

    public async Task<OwnerStatementListBundleData> GetOwnerStatementListDataAsync(JournalEntryRecapGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (rowRaw, outstandingRaw) = await db.DapperProcQueryMultipleAsync<
            OwnerStatementListRowEntity,
            OwnerInvoiceOutstandingEntity>(
            OwnerStatementListProcName,
            new
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = criteria.OfficeIds,
                PropertyId = criteria.PropertyId,
                StartDate = criteria.StartDate,
                EndDate = criteria.EndDate,
                IncludeUnposted = criteria.IncludeUnposted
            },
            commandTimeout: 120);

        return new OwnerStatementListBundleData
        {
            Rows = (rowRaw ?? []).Select(ConvertOwnerStatementListRowEntityToModel).ToList(),
            OutstandingInvoices = (outstandingRaw ?? [])
                .Select(ConvertOwnerStatementListOutstandingEntityToModel)
                .ToList()
        };
    }

    private static OwnerInvoiceOutstanding ConvertOwnerStatementListOutstandingEntityToModel(OwnerInvoiceOutstandingEntity entity)
        => new()
        {
            OwnerInvoiceOutstandingId = entity.OwnerInvoiceOutstandingId,
            OrganizationId = entity.OrganizationId,
            OfficeId = entity.OfficeId,
            PropertyId = entity.PropertyId,
            InvoiceId = entity.InvoiceId,
            InvoiceCode = entity.InvoiceCode,
            AccountingPeriod = DateOnly.FromDateTime(entity.AccountingPeriod),
            Description = entity.Description,
            ExpectedAmount = entity.ExpectedAmount,
            ActualAmount = entity.ActualAmount,
            Outstanding = entity.Outstanding,
            ModifiedOn = entity.ModifiedOn
        };

    private static OwnerCashReportRow ConvertOwnerStatementListRowEntityToModel(OwnerStatementListRowEntity entity)
    {
        return new OwnerCashReportRow
        {
            PropertyId = entity.PropertyId,
            OfficeId = entity.OfficeId,
            OfficeName = entity.OfficeName,
            OwnerId = entity.OwnerId,
            PropertyCode = entity.PropertyCode,
            CompanyName = entity.CompanyName,
            OwnerNames = entity.OwnerNames,
            OwnerNameLine = entity.OwnerNameLine,
            StartingBalance = entity.StartingBalance,
            ReceivedIncome = entity.ReceivedIncome,
            OwnerExpenses = entity.OwnerExpenses,
            OwnerPayment = entity.OwnerPayment,
            OwnerPaymentPaid = entity.OwnerPaymentPaid,
            EndingBalance = entity.EndingBalance,
            WorkingCapital = entity.WorkingCapital
        };
    }
}

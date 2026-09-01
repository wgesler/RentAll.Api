using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    public async Task RecalculateOwnerInvoiceOutstandingForSliceAsync(OwnerInvoiceOutstandingSliceKey sliceKey)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.OwnerInvoiceOutstanding_RecalculateForSlice", new
        {
            OrganizationId = sliceKey.OrganizationId,
            OfficeId = sliceKey.OfficeId,
            PropertyId = sliceKey.PropertyId,
            InvoiceId = sliceKey.InvoiceId,
            AccountingPeriod = sliceKey.AccountingPeriod.ToDateTime(TimeOnly.MinValue)
        });
    }

    public async Task<IReadOnlyList<OwnerInvoiceOutstanding>> GetOwnerInvoiceOutstandingByCriteriaAsync(Guid organizationId, Guid? propertyId = null, string? officeIds = null, DateOnly? endDate = null)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var entities = await db.DapperProcQueryAsync<OwnerInvoiceOutstandingEntity>(
            "Accounting.OwnerInvoiceOutstanding_GetByCriteria",
            new
            {
                OrganizationId = organizationId,
                PropertyId = propertyId,
                OfficeIds = officeIds,
                EndDate = endDate?.ToDateTime(TimeOnly.MinValue)
            });

        return (entities ?? Enumerable.Empty<OwnerInvoiceOutstandingEntity>())
            .Select(ConvertOwnerInvoiceOutstandingEntityToModel)
            .ToList();
    }

    public async Task<IReadOnlyList<OwnerInvoiceOutstanding>> GetOwnerInvoiceOutstandingByPropertyIdAsync(Guid organizationId, Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var entities = await db.DapperProcQueryAsync<OwnerInvoiceOutstandingEntity>(
            "Accounting.OwnerInvoiceOutstanding_GetOutstandingByPropertyId",
            new
            {
                OrganizationId = organizationId,
                PropertyId = propertyId
            });

        return (entities ?? Enumerable.Empty<OwnerInvoiceOutstandingEntity>())
            .Select(ConvertOwnerInvoiceOutstandingEntityToModel)
            .ToList();
    }

    public async Task DeleteOwnerInvoiceOutstandingByInvoiceIdAsync(Guid organizationId, Guid invoiceId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.OwnerInvoiceOutstanding_DeleteByInvoiceId", new
        {
            OrganizationId = organizationId,
            InvoiceId = invoiceId
        });
    }

    public async Task BackfillOwnerInvoiceOutstandingAsync(Guid? organizationId = null, int? officeId = null)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.OwnerInvoiceOutstanding_Backfill", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });
    }

    private static OwnerInvoiceOutstanding ConvertOwnerInvoiceOutstandingEntityToModel(OwnerInvoiceOutstandingEntity entity)
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
}

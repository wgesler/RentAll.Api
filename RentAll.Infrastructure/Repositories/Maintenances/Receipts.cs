using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region Selects
    public async Task<IEnumerable<Receipt>> GetReceiptsByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReceiptEntity>("Maintenance.Receipt_GetListByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Receipt>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<Receipt>> GetReceiptsByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReceiptEntity>("Maintenance.Receipt_GetListByPropertyId", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Receipt>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Receipt?> GetReceiptByIdAsync(int receiptId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReceiptEntity>("Maintenance.Receipt_GetById", new
        {
            ReceiptId = receiptId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Creates
    public async Task<Receipt> CreateReceiptAsync(Receipt receipt)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReceiptEntity>("Maintenance.Receipt_Add", new
        {
            OrganizationId = receipt.OrganizationId,
            OfficeId = receipt.OfficeId,
            Properties = SerializeReceiptPropertyIds(receipt.PropertyIds),
            Amount = receipt.Amount,
            Description = receipt.Description,
            Splits = SerializeReceiptSplits(receipt.Splits),
            ReceiptPath = receipt.ReceiptPath,
            IsActive = receipt.IsActive,
            CreatedBy = receipt.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Receipt record not created");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Updates
    public async Task<Receipt> UpdateReceiptAsync(Receipt receipt)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReceiptEntity>("Maintenance.Receipt_UpdateById", new
        {
            ReceiptId = receipt.ReceiptId,
            OrganizationId = receipt.OrganizationId,
            OfficeId = receipt.OfficeId,
            Properties = SerializeReceiptPropertyIds(receipt.PropertyIds),
            Amount = receipt.Amount,
            Description = receipt.Description,
            Splits = SerializeReceiptSplits(receipt.Splits),
            ReceiptPath = receipt.ReceiptPath,
            IsActive = receipt.IsActive,
            ModifiedBy = receipt.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Receipt record not found");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Deletes
    public async Task DeleteReceiptByIdAsync(int receiptId, Guid organizationId, Guid currentUser)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Receipt_DeleteById", new
        {
            ReceiptId = receiptId,
            OrganizationId = organizationId,
            ModifiedBy = currentUser
        });
    }
    #endregion
}

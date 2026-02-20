using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Create
    public async Task<CostCode> CreateAsync(CostCode costCode)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<CostCodeEntity>("Accounting.CostCode_Add", new
        {
            OrganizationId = costCode.OrganizationId,
            OfficeId = costCode.OfficeId,
            CostCode = costCode.Code,
            TransactionTypeId = (int)costCode.TransactionType,
            Description = costCode.Description,
            IsActive = costCode.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("CostCode not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Select
    public async Task<List<CostCode>> GetAllAsync(string officeIds, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<CostCodeEntity>("Accounting.CostCode_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeIds
        });

        if (res == null || !res.Any())
            return new List<CostCode>();

        return res.Select(ConvertEntityToModel).ToList();
    }

    public async Task<List<CostCode>> GetAllByOfficeIdAsync(int officeId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<CostCodeEntity>("Accounting.CostCode_GetAllByOfficeId", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return new List<CostCode>();

        return res.Select(ConvertEntityToModel).ToList();
    }

    public async Task<CostCode?> GetByIdAsync(int costCodeId, int officeId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<CostCodeEntity>("Accounting.CostCode_GetById", new
        {
            CostCodeId = costCodeId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<CostCode?> GetByCostCodeAsync(string costCode, int officeId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<CostCodeEntity>("Accounting.CostCode_GetByCostCode", new
        {
            CostCode = costCode,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<bool> ExistsByCostCodeAsync(string costCode, int officeId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryScalarAsync<int>("Accounting.CostCode_ExistsByCode", new
        {
            CostCode = costCode,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        return result == 1;
    }
    #endregion

    #region Update
    public async Task<CostCode> UpdateByIdAsync(CostCode costCode)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<CostCodeEntity>("Accounting.CostCode_UpdateById", new
        {
            CostCodeId = costCode.CostCodeId,
            OrganizationId = costCode.OrganizationId,
            OfficeId = costCode.OfficeId,
            CostCode = costCode.Code,
            TransactionTypeId = (int)costCode.TransactionType,
            Description = costCode.Description,
            IsActive = costCode.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("CostCode not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Delete
    public async Task DeleteByIdAsync(int costCodeId, int officeId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.CostCode_DeleteById", new
        {
            CostCodeId = costCodeId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });
    }
    #endregion
}

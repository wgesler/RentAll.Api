using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Selects
    public async Task<List<CostCode>> GetCostCodesByOfficeIdsAsync(Guid organizationId, string officeIds)
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

    public async Task<List<CostCode>> GetCostCodesByOfficeIdAsync(Guid organizationId, int officeId)
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

    public async Task<CostCode?> GetCostCodeByIdAsync(int costCodeId, Guid organizationId, int officeId)
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

    public async Task<CostCode?> GetByCostCodeAsync(string costCode, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<CostCodeEntity>("Accounting.CostCode_GetByCode", new
        {
            CostCode = costCode,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<bool> ExistsByCostCodeAsync(string costCode, Guid organizationId, int officeId)
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

    #region Creates
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

    #region Updates
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

    #region Deletes
    public async Task DeleteCostCodeByIdAsync(int costCodeId, Guid organizationId, int officeId)
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

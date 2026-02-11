using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.CostCodes;

public partial class CostCodeRepository : ICostCodeRepository
{
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
}

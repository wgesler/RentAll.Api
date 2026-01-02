using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Offices;

public partial class OfficeRepository : IOfficeRepository
{
	public async Task<IEnumerable<Office>> GetAllAsync(Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<OfficeEntity>("dbo.Office_GetAll", new
		{
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Office>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<Office?> GetByIdAsync(int officeId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<OfficeEntity>("dbo.Office_GetById", new
		{
			OfficeId = officeId,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<Office?> GetByOfficeCodeAsync(string officeCode, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<OfficeEntity>("dbo.Office_GetByCode", new
		{
			OfficeCode = officeCode,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<bool> ExistsByOfficeCodeAsync(string officeCode, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var result = await db.DapperProcQueryScalarAsync<int>("dbo.Office_ExistsByCode", new
		{
			OfficeCode = officeCode,
			OrganizationId = organizationId
		});

		return result == 1;
	}
}


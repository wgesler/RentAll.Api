using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository : IOrganizationRepository
{
	public async Task<IEnumerable<Organization>> GetAllAsync()
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<OrganizationEntity>("Organization.Organization_GetAll", null);

		if (res == null || !res.Any())
			return Enumerable.Empty<Organization>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<Organization?> GetByIdAsync(Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<OrganizationEntity>("Organization.Organization_GetById", new
		{
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<Organization?> GetByOrganizationCodeAsync(string organizationCode)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<OrganizationEntity>("Organization.Organization_GetByOrganizationCode", new
		{
			OrganizationCode = organizationCode
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<bool> ExistsByOrganizationCodeAsync(string organizationCode)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<OrganizationEntity>("Organization.Organization_GetByOrganizationCode", new
		{
			OrganizationCode = organizationCode
		});

		return res != null && res.Any();
	}
}





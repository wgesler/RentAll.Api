using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.OfficeConfigurations;

public partial class OfficeConfigurationRepository : IOfficeConfigurationRepository
{
	public async Task<IEnumerable<OfficeConfiguration>> GetAllAsync(Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<OfficeConfigurationEntity>("dbo.OfficeConfiguration_GetAll", new
		{
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<OfficeConfiguration>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<OfficeConfiguration?> GetByOfficeIdAsync(int officeId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<OfficeConfigurationEntity>("dbo.OfficeConfiguration_GetByOfficeId", new
		{
			OfficeId = officeId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}


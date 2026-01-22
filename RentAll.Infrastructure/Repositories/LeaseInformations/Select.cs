using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.LeaseInformations
{
	public partial class LeaseInformationRepository : ILeaseInformationRepository
	{
		public async Task<LeaseInformation?> GetByIdAsync(Guid propertyId, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<LeaseInformationEntity>("Property.LeaseInformation_GetById", new
			{
				PropertyId = propertyId,
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}

		public async Task<LeaseInformation?> GetByPropertyIdAsync(Guid propertyId, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<LeaseInformationEntity>("Property.LeaseInformation_GetByPropertyId", new
			{
				PropertyId = propertyId,
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}


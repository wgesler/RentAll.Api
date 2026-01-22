using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository : IPropertyRepository
    {
		// Property Lists
		public async Task<IEnumerable<PropertyList>> GetListByOfficeIdAsync(Guid organizationId, string officeAccess)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<PropertyListEntity>("Property.Property_GetListByOfficeId", new
			{
				OrganizationId = organizationId,
				Offices = officeAccess
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<PropertyList>();

			return res.Select(ConvertEntityToModel);
		}

		public async Task<IEnumerable<PropertyList>> GetListBySelectionCriteriaAsync(Guid userId, Guid organizationId, string officeAccess)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<PropertyListEntity>("Property.Property_GetListBySelection", new
			{
				UserId = userId,
                OrganizationId = organizationId,
				Offices = officeAccess
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<PropertyList>();

			return res.Select(ConvertEntityToModel);
		}

		// Individual Properties
		public async Task<Property?> GetByIdAsync(Guid propertyId, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<PropertyEntity>("Property.Property_GetById", new
			{
				PropertyId = propertyId,
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}

		public async Task<Property?> GetByPropertyCodeAsync(string propertyCode, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<PropertyEntity>("Property.Property_GetByCode", new
			{
				PropertyCode = propertyCode,
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}

		public async Task<bool> ExistsByPropertyCodeAsync(string propertyCode, Guid organizationId)
        {
			await using var db = new SqlConnection(_dbConnectionString);
			var result = await db.DapperProcQueryScalarAsync<int>("Property.Property_ExistsByCode", new
			{
				PropertyCode = propertyCode,
				OrganizationId = organizationId
			});

			return result == 1;
		}
    }
}

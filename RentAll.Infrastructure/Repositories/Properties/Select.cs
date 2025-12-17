using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository : IPropertyRepository
    {
		public async Task<IEnumerable<Property>> GetAllAsync(Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<PropertyEntity>("dbo.Property_GetAll", new
			{
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<Property>();

			return res.Select(ConvertEntityToModel);
		}

		public async Task<Property?> GetByIdAsync(Guid propertyId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyEntity>("dbo.Property_GetById", new
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
            var res = await db.DapperProcQueryAsync<PropertyEntity>("dbo.Property_GetByCode", new
            {
                PropertyCode = propertyCode,
				OrganizationId = organizationId
			});

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<IEnumerable<Property>> GetByStateAsync(string state, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyEntity>("dbo.Property_GetByState", new
            {
                State = state,
				OrganizationId = organizationId
			});

            if (res == null || !res.Any())
                return Enumerable.Empty<Property>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<bool> ExistsByPropertyCodeAsync(string propertyCode, Guid organizationId)
        {
			await using var db = new SqlConnection(_dbConnectionString);
			var result = await db.DapperProcQueryScalarAsync<int>("dbo.Property_ExistsByCode", new
			{
				PropertyCode = propertyCode,
				OrganizationId = organizationId
			});

			return result == 1;
		}
    }
}
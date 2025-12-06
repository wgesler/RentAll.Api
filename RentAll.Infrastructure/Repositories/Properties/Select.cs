using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository : IPropertyRepository
    {
        public async Task<Property?> GetByIdAsync(Guid propertyId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyEntity>("dbo.Property_GetById", new
            {
                PropertyId = propertyId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertDtoToModel(res.FirstOrDefault()!);
        }

        public async Task<Property?> GetByPropertyCodeAsync(string propertyCode)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyEntity>("dbo.Property_GetByPropertyCode", new
            {
                PropertyCode = propertyCode
            });

            if (res == null || !res.Any())
                return null;

            return ConvertDtoToModel(res.FirstOrDefault()!);
        }

        public async Task<IEnumerable<Property>> GetAllAsync()
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyEntity>("dbo.Property_GetAll", null);

            if (res == null || !res.Any())
                return Enumerable.Empty<Property>();

            return res.Select(ConvertDtoToModel);
        }

        public async Task<IEnumerable<Property>> GetByStateAsync(string state)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyEntity>("dbo.Property_GetByState", new
            {
                State = state
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Property>();

            return res.Select(ConvertDtoToModel);
        }

        public async Task<bool> ExistsByPropertyCodeAsync(string propertyCode)
        {
			await using var db = new SqlConnection(_dbConnectionString);
			var result = await db.DapperProcQueryScalarAsync<int>("dbo.Property_ExistsByCode", new
			{
				PropertyCode = propertyCode
			});

			return result == 1;
		}
    }
}
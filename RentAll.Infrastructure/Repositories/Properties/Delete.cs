using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository : IPropertyRepository
    {
        public async Task DeleteByIdAsync(Guid propertyId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("dbo.Property_DeleteById", new
            {
                PropertyId = propertyId
            });
        }
    }
}
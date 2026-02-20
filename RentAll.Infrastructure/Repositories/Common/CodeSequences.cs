using Microsoft.Data.SqlClient;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Common
{
    public partial class CommonRepository
    {
        #region Select
        public async Task<int> GetNextAsync(Guid organizationId, int entityTypeId, string entityType)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var result = await db.DapperProcQueryScalarAsync<int>("Organization.CodeSequence_GetNext", new
            {
                OrganizationId = organizationId,
                EntityTypeId = entityTypeId,
                EntityType = entityType
            });

            return result;
        }
        #endregion
    }
}

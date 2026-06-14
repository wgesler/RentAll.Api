using Microsoft.Data.SqlClient;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Common
{
    public partial class CommonRepository
    {
        #region Selects
        public async Task<int> GetNextCodeAsync(Guid organizationId, int entityTypeId, string entityType)
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

        public async Task ResetCodeSequenceAsync(Guid organizationId, int entityTypeId, string entityType, int nextNumber = 0)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Organization.CodeSequence_Reset", new
            {
                OrganizationId = organizationId,
                EntityTypeId = entityTypeId,
                EntityType = entityType,
                NextNumber = nextNumber
            });
        }
        #endregion
    }
}

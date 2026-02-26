using Microsoft.Data.SqlClient;
using RentAll.Domain.Models.Common;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Common
{
    public partial class CommonRepository
    {
        #region Select
        public async Task<IEnumerable<State>> GetAllStatesAsync()
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<StateEntity>("Organization.State_GetAll", null);

            if (res == null || !res.Any())
                return Enumerable.Empty<State>();

            return res.Select(ConvertEntityToModel);
        }
        #endregion
    }
}

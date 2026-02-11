using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Common;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Common
{
	public partial class CommonRepository : ICommonRepository
	{
		public async Task<IEnumerable<State>> GetAllStatesAsync()
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<StateEntity>("Organization.State_GetAll", null);

			if (res == null || !res.Any())
				return Enumerable.Empty<State>();

			return res.Select(ConvertEntityToModel);
		}
	}
}


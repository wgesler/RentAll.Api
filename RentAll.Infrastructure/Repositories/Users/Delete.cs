using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Users
{
	public partial class UserRepository : IUserRepository
	{
		public async Task DeleteByIdAsync(Guid userId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			await db.DapperProcExecuteAsync("dbo.User_DeleteById", new
			{
				UserId = userId
			});
		}
	}
}
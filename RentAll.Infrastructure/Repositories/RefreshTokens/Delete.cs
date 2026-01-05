using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.RefreshTokens
{
	public partial class RefreshTokenRepository : IRefreshTokenRepository
	{
		public async Task DeleteByIdAsync(Guid refreshTokenId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			await db.DapperProcExecuteAsync("dbo.RefreshToken_DeleteById", new
			{
				RefreshTokenId = refreshTokenId
			});
		}

		public async Task DeleteExpiredAsync()
		{
			await using var db = new SqlConnection(_dbConnectionString);
			await db.DapperProcExecuteAsync("dbo.RefreshToken_DeleteExpired", null);
		}
	}
}

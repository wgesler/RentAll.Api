using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.RefreshTokens
{
	public partial class RefreshTokenRepository : IRefreshTokenRepository
	{
		public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<RefreshTokenEntity>("dbo.RefreshToken_Add", new
			{
				UserId = refreshToken.UserId,
				TokenHash = refreshToken.TokenHash,
				ExpiresOn = refreshToken.ExpiresOn
			});

			if (res == null || !res.Any())
				throw new Exception("RefreshToken not created");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}

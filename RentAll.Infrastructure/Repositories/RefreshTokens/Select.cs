using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.RefreshTokens
{
	public partial class RefreshTokenRepository : IRefreshTokenRepository
	{
		public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<RefreshTokenEntity>("dbo.RefreshToken_GetByTokenHash", new
			{
				TokenHash = tokenHash
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}

		public async Task<RefreshToken?> GetByIdAsync(Guid refreshTokenId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<RefreshTokenEntity>("dbo.RefreshToken_GetById", new
			{
				RefreshTokenId = refreshTokenId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}

		public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<RefreshTokenEntity>("dbo.RefreshToken_GetByUserId", new
			{
				UserId = userId
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<RefreshToken>();

			return res.Select(ConvertEntityToModel);
		}

		public async Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<RefreshTokenEntity>("dbo.RefreshToken_GetActiveByUserId", new
			{
				UserId = userId
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<RefreshToken>();

			return res.Select(ConvertEntityToModel);
		}
	}
}
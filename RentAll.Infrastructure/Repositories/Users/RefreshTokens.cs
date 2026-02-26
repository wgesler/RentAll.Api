using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Users
{
    public partial class UserRepository
    {
        #region Create
        public async Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<RefreshTokenEntity>("User.RefreshToken_Add", new
            {
                UserId = refreshToken.UserId,
                TokenHash = refreshToken.TokenHash,
                ExpiresOn = refreshToken.ExpiresOn
            });

            if (res == null || !res.Any())
                throw new Exception("RefreshToken not created");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Select
        public async Task<RefreshToken?> GetRefreshTokenByTokenHashAsync(string tokenHash)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<RefreshTokenEntity>("User.RefreshToken_GetByTokenHash", new
            {
                TokenHash = tokenHash
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<RefreshToken?> GetRefreshTokenByIdAsync(Guid refreshTokenId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<RefreshTokenEntity>("User.RefreshToken_GetById", new
            {
                RefreshTokenId = refreshTokenId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<IEnumerable<RefreshToken>> GetRefreshTokensByUserIdAsync(Guid userId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<RefreshTokenEntity>("User.RefreshToken_GetByUserId", new
            {
                UserId = userId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<RefreshToken>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<RefreshToken>> GetActiveRefreshTokensByUserIdAsync(Guid userId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<RefreshTokenEntity>("User.RefreshToken_GetActiveByUserId", new
            {
                UserId = userId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<RefreshToken>();

            return res.Select(ConvertEntityToModel);
        }
        #endregion

        #region Delete
        public async Task DeleteRefreshTokenByIdAsync(Guid refreshTokenId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("User.RefreshToken_DeleteById", new
            {
                RefreshTokenId = refreshTokenId
            });
        }

        public async Task DeleteExpiredRefreshTokensAsync()
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("User.RefreshToken_DeleteExpired", null);
        }
        #endregion
    }
}

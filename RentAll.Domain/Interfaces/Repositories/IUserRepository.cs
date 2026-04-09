using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        #region Users
        Task<IEnumerable<User>> GetUsersByOrganizationIdAsync(Guid organizationId);
        Task<IEnumerable<User>> GetUsersByRoleTypeAsync(Guid organizationId, string roleType);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email);

        Task<User> CreateAsync(User user);
        Task<User> UpdateByIdAsync(User user);
        Task DeleteUserByIdAsync(Guid userId);
        #endregion

        #region Refresh Tokens
        Task<RefreshToken?> GetRefreshTokenByTokenHashAsync(string tokenHash);
        Task<RefreshToken?> GetRefreshTokenByIdAsync(Guid refreshTokenId);
        Task<IEnumerable<RefreshToken>> GetRefreshTokensByUserIdAsync(Guid userId);
        Task<IEnumerable<RefreshToken>> GetActiveRefreshTokensByUserIdAsync(Guid userId);

        Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken);
        Task DeleteRefreshTokenByIdAsync(Guid refreshTokenId);
        Task DeleteExpiredRefreshTokensAsync();
        #endregion
    }
}

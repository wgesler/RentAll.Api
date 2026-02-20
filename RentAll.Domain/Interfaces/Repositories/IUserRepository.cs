using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        // User Creates
        Task<User> CreateAsync(User user);

        // User Selects
        Task<IEnumerable<User>> GetAllAsync(Guid organizationId);
        Task<User?> GetByIdAsync(Guid userId);
        Task<User?> GetByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email);

        // User Updates
        Task<User> UpdateByIdAsync(User user);

        // User Deletes
        Task DeleteByIdAsync(Guid userId);

        // RefreshToken Creates
        Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken);

        // RefreshToken Selects
        Task<RefreshToken?> GetRefreshTokenByTokenHashAsync(string tokenHash);
        Task<RefreshToken?> GetRefreshTokenByIdAsync(Guid refreshTokenId);
        Task<IEnumerable<RefreshToken>> GetRefreshTokensByUserIdAsync(Guid userId);
        Task<IEnumerable<RefreshToken>> GetActiveRefreshTokensByUserIdAsync(Guid userId);

        // RefreshToken Deletes
        Task DeleteRefreshTokenByIdAsync(Guid refreshTokenId);
        Task DeleteExpiredRefreshTokensAsync();
    }
}

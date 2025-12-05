using RentAll.Domain.Models.Auth;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
	// Creates
	Task<RefreshToken> CreateAsync(RefreshToken refreshToken);

	// Selects
	Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
	Task<RefreshToken?> GetByIdAsync(Guid refreshTokenId);
	Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId);
	Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId);

	// Deletes
	Task DeleteByIdAsync(Guid refreshTokenId);
	Task DeleteExpiredAsync();
}


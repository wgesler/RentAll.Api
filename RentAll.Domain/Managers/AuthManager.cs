using System.Security.Cryptography;
using System.Text;
using RentAll.Domain.Interfaces.Auth;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public class AuthManager
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthTokenService _tokenService;

    public AuthManager(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthTokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<(bool Success, User? User, string? AccessToken, string? RefreshToken)> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
            return (false, null, null, null);

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            return (false, null, null, null);

        if (!user.IsActive)
            return (false, null, null, null);

        var accessToken = _tokenService.GenerateToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.UserId);

        return (true, user, accessToken, refreshToken);
    }

    public async Task<(bool Success, User? User, string? AccessToken, string? RefreshToken, string? ErrorMessage)> RegisterAsync(Guid organizationId, string firstName, string lastName, string email, string password)
    {
        if (await _userRepository.ExistsByEmailAsync(email))
            return (false, null, null, null, "Email already exists");

        var passwordHash = _passwordHasher.HashPassword(password);
        var user = new User
        {
            OrganizationId = organizationId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedBy = Guid.Empty
        };

        var createdUser = await _userRepository.CreateAsync(user);
        var accessToken = _tokenService.GenerateToken(createdUser);
        var refreshToken = await CreateRefreshTokenAsync(createdUser.UserId);

        return (true, createdUser, accessToken, refreshToken, null);
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId)
    {
        var refreshToken = GenerateRefreshToken();
        var tokenHash = HashRefreshToken(refreshToken);

        var storedToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(7), // 7 days expiration
            CreatedOn = DateTimeOffset.UtcNow
        };

        await _userRepository.CreateRefreshTokenAsync(storedToken);
        return refreshToken;
    }

    public async Task<(bool Success, User? User, string? AccessToken, string? RefreshToken, string? ErrorMessage)> RefreshTokenAsync(string refreshToken)
    {
        // Hash the provided refresh token to look it up
        var tokenHash = HashRefreshToken(refreshToken);

        // Look up the refresh token
        var storedToken = await _userRepository.GetRefreshTokenByTokenHashAsync(tokenHash);
        if (storedToken == null || !storedToken.IsActive)
            return (false, null, null, null, "Invalid or expired refresh token");

        // Get the user
        var user = await _userRepository.GetByIdAsync(storedToken.UserId);
        if (user == null)
            return (false, null, null, null, "User not found");

        // Generate new access token
        var accessToken = _tokenService.GenerateToken(user);

        // Generate new refresh token
        var newRefreshToken = GenerateRefreshToken();
        var newTokenHash = HashRefreshToken(newRefreshToken);

        // Create new refresh token in database
        var newStoredToken = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = user.UserId,
            TokenHash = newTokenHash,
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(7), // 7 days expiration
            CreatedOn = DateTimeOffset.UtcNow
        };

        await _userRepository.CreateRefreshTokenAsync(newStoredToken);

        // Delete the old refresh token
        await _userRepository.DeleteRefreshTokenByIdAsync(storedToken.RefreshTokenId);

        return (true, user, accessToken, newRefreshToken, null);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        // Hash the provided refresh token to look it up
        var tokenHash = HashRefreshToken(refreshToken);

        // Look up the refresh token
        var storedToken = await _userRepository.GetRefreshTokenByTokenHashAsync(tokenHash);
        if (storedToken == null)
            return false; // Token not found, but we'll return success to avoid information leakage

        // Delete the refresh token
        await _userRepository.DeleteRefreshTokenByIdAsync(storedToken.RefreshTokenId);
        return true;
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdatePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
            return (false, "Current password is required");

        if (string.IsNullOrWhiteSpace(newPassword))
            return (false, "New password is required");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return (false, "User not found");

        if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
            return (false, "Current password is incorrect");

        var newPasswordHash = _passwordHasher.HashPassword(newPassword);
        user.PasswordHash = newPasswordHash;
        user.ModifiedBy = userId; // User is updating their own password

        await _userRepository.UpdateByIdAsync(user);
        return (true, null);
    }

    private static string HashRefreshToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}



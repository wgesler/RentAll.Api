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

    public async Task<(bool Success, User? User, string? Token)> LoginAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        
        if (user == null)
        {
            return (false, null, null);
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            return (false, null, null);
        }

        var token = _tokenService.GenerateToken(user);
        return (true, user, token);
    }

    public async Task<(bool Success, User? User, string? ErrorMessage)> RegisterAsync(string username, string firstName, string lastName, string email, string password)
    {
        if (await _userRepository.ExistsByUsernameAsync(username))
        {
            return (false, null, "Username already exists");
        }

        if (await _userRepository.ExistsByEmailAsync(email))
        {
            return (false, null, "Email already exists");
        }

        var passwordHash = _passwordHasher.HashPassword(password);
        var fullName = $"{firstName} {lastName}".Trim();
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            IsActive = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(user);
        return (true, createdUser, null);
    }
}



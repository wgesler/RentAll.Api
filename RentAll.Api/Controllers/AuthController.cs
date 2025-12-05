using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Auth;
using RentAll.Domain.Interfaces.Auth;
using RentAll.Domain.Managers;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthManager _authManager;
    private readonly IAuthTokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthManager authManager, 
        IAuthTokenService tokenService, 
        ILogger<AuthController> logger)
    {
        _authManager = authManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
        {
            return BadRequest(new { message = "Username and password are required" });
        }

        var (success, user, token) = await _authManager.LoginAsync(loginDto.Username, loginDto.Password);

        if (!success || user == null || token == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var response = new AuthResponseDto
        {
            Token = token,
            Username = user.Username,
            Email = user.Email
        };

        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (string.IsNullOrWhiteSpace(registerDto.Username) || 
            string.IsNullOrWhiteSpace(registerDto.FirstName) ||
            string.IsNullOrWhiteSpace(registerDto.LastName) ||
            string.IsNullOrWhiteSpace(registerDto.Email) || 
            string.IsNullOrWhiteSpace(registerDto.Password))
        {
            return BadRequest(new { message = "Username, first name, last name, email, and password are required" });
        }

        var (success, user, errorMessage) = await _authManager.RegisterAsync(
            registerDto.Username,
            registerDto.FirstName,
            registerDto.LastName,
            registerDto.Email, 
            registerDto.Password);

        if (!success || user == null)
        {
            return BadRequest(new { message = errorMessage ?? "Registration failed" });
        }

        var token = _tokenService.GenerateToken(user);
        var response = new AuthResponseDto
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
			LastName = user.LastName
		};

        return CreatedAtAction(nameof(Login), response);
    }
}


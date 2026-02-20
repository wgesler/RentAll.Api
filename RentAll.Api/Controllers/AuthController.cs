using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Auth;
using RentAll.Domain.Managers;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public partial class AuthController : ControllerBase
{
    protected readonly AuthManager _authManager;
    protected readonly IAuthTokenService _tokenService;
    protected readonly IConfiguration _configuration;
    protected readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthManager authManager,
        IAuthTokenService tokenService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _authManager = authManager;
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;
    }

    protected int GetExpiresInSeconds()
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");
        return expirationMinutes * 60; // Convert minutes to seconds
    }
}

using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Auth;

namespace RentAll.Api.Controllers;

public partial class AuthController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
            return BadRequest(new { message = "Username and password are required" });

        var (success, user, accessToken, refreshToken) = await _authManager.LoginAsync(loginDto.Username, loginDto.Password);

        if (!success || user == null || accessToken == null || refreshToken == null)
            return Unauthorized(new { message = "Invalid username or password" });

        var expiresIn = GetExpiresInSeconds();
        var response = new AuthResponseDto(accessToken, refreshToken, expiresIn);
        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (string.IsNullOrWhiteSpace(registerDto.Username) || string.IsNullOrWhiteSpace(registerDto.FirstName) ||
            string.IsNullOrWhiteSpace(registerDto.LastName) || string.IsNullOrWhiteSpace(registerDto.Email) || 
            string.IsNullOrWhiteSpace(registerDto.Password))
            return BadRequest(new { message = "Username, first, last, email, and password are required" });

        var (success, user, accessToken, refreshToken, errorMessage) = await _authManager.RegisterAsync(registerDto.Username,
            registerDto.FirstName, registerDto.LastName, registerDto.Email, registerDto.Password);

        if (!success || user == null || accessToken == null || refreshToken == null)
            return BadRequest(new { message = errorMessage ?? "Registration failed" });

        var expiresIn = GetExpiresInSeconds();
        var response = new AuthResponseDto(accessToken, refreshToken, expiresIn);
        return CreatedAtAction(nameof(Login), response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto refreshTokenDto)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var (success, user, accessToken, refreshToken, errorMessage) = await _authManager.RefreshTokenAsync(refreshTokenDto.RefreshToken);

        if (!success || user == null || accessToken == null || refreshToken == null)
            return Unauthorized(new { message = errorMessage ?? "Invalid refresh token" });

        var expiresIn = GetExpiresInSeconds();
        var response = new AuthResponseDto(accessToken, refreshToken, expiresIn);
        return Ok(response);
    }
}
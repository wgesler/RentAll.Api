using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Auth;

namespace RentAll.Api.Controllers;

public partial class AuthController
{
    #region Post

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
            return BadRequest("Username and password are required");

        var (success, user, accessToken, refreshToken) = await _authManager.LoginAsync(loginDto.Username, loginDto.Password);

        if (!success || user == null || accessToken == null || refreshToken == null)
            return Unauthorized("Invalid username or password");

        var expiresIn = GetExpiresInSeconds();
        var response = new AuthResponseDto(accessToken, refreshToken, expiresIn);
        return Ok(response);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (string.IsNullOrWhiteSpace(registerDto.FirstName) || string.IsNullOrWhiteSpace(registerDto.LastName) ||
            string.IsNullOrWhiteSpace(registerDto.Email) || string.IsNullOrWhiteSpace(registerDto.Password))
            return BadRequest("First, last, email, and password are required");

        var (success, user, accessToken, refreshToken, errorMessage) = await _authManager.RegisterAsync(
            registerDto.OrganizationId, registerDto.FirstName, registerDto.LastName, registerDto.Email, registerDto.Password);

        if (!success || user == null || accessToken == null || refreshToken == null)
            return BadRequest(errorMessage ?? "Registration failed");

        var expiresIn = GetExpiresInSeconds();
        var response = new AuthResponseDto(accessToken, refreshToken, expiresIn);
        return CreatedAtAction(nameof(Login), response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto refreshTokenDto)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken))
            return BadRequest("Refresh token is required");

        var (success, user, accessToken, refreshToken, errorMessage) = await _authManager.RefreshTokenAsync(refreshTokenDto.RefreshToken);

        if (!success || user == null || accessToken == null || refreshToken == null)
            return Unauthorized(errorMessage ?? "Invalid refresh token");

        var expiresIn = GetExpiresInSeconds();
        var response = new AuthResponseDto(accessToken, refreshToken, expiresIn);
        return Ok(response);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto refreshTokenDto)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken))
            return BadRequest("Refresh token is required");

        var success = await _authManager.LogoutAsync(refreshTokenDto.RefreshToken);

        if (!success)
            return BadRequest("Failed to logout");

        return Ok(new { message = "Logged out successfully" });
    }

    #endregion

    #region Put

    /// <summary>
    /// Update the current user's password.
    /// </summary>
    [HttpPut("password")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
    {
        if (dto == null)
            return BadRequest("Password data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var currentUserId = CurrentUser;
            if (currentUserId == Guid.Empty)
                return Unauthorized("User not authenticated");

            var (success, updateError) = await _authManager.UpdatePasswordAsync(currentUserId, dto.Password, dto.NewPassword);

            if (!success)
                return BadRequest(updateError ?? "Failed to update password");

            return Ok(new { message = "Password updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password");
            return StatusCode(500, new { message = "An error occurred while updating the password" });
        }
    }

    #endregion
}

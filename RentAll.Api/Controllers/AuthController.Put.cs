using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Auth;
using System.Text;
using System.Text.Json;

namespace RentAll.Api.Controllers;

public partial class AuthController
{
    /// <summary>
    /// Update the current user's password
    /// </summary>
    /// <param name="dto">UpdatePasswordDto containing Password (current) and NewPassword</param>
    /// <returns>Success message</returns>
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
            var currentUserId = GetCurrentUserId();
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

    private Guid GetCurrentUserId()
    {
        if (User?.Identity?.IsAuthenticated != true)
            return Guid.Empty;

        var userClaim = User.FindFirst("user");
        if (userClaim == null || string.IsNullOrWhiteSpace(userClaim.Value))
            return Guid.Empty;

        try
        {
            var userJsonBytes = Convert.FromBase64String(userClaim.Value);
            var userJson = Encoding.UTF8.GetString(userJsonBytes);
            var userObject = JsonSerializer.Deserialize<JsonElement>(userJson);

            if (userObject.TryGetProperty("userId", out var userIdElement))
            {
                var userIdString = userIdElement.GetString();
                if (!string.IsNullOrWhiteSpace(userIdString) && Guid.TryParse(userIdString, out var userId))
                    return userId;
            }
        }
        catch
        {
            // If decoding fails, return empty GUID
        }

        return Guid.Empty;
    }
}


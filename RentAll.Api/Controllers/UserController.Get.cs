using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Users;

namespace RentAll.Api.Controllers
{
    public partial class UserController
    {
        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { message = "User ID is required" });

            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                return Ok(new UserResponseDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the user" });
            }
        }

        /// <summary>
        /// Get user by username
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>User</returns>
        [HttpGet("username/{username}")]
        public async Task<IActionResult> GetByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { message = "Username is required" });

            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                return Ok(new UserResponseDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username: {Username}", username);
                return StatusCode(500, new { message = "An error occurred while retrieving the user" });
            }
        }
    }
}


using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Users;

namespace RentAll.Api.Controllers
{
    public partial class UserController
    {
		/// <summary>
		/// Get all users
		/// </summary>
		/// <returns>List of users</returns>
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var users = await _userRepository.GetAllAsync();
				var response = users.Select(u => new UserResponseDto(u));
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all users");
				return StatusCode(500, new { message = "An error occurred while retrieving users" });
			}
		}
		
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
    }
}






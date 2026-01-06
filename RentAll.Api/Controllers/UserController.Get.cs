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
				var users = await _userRepository.GetAllAsync(CurrentOrganizationId);
				var response = users.Select(u => new UserResponseDto(u));
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all users");
				return ServerError("An error occurred while retrieving users");
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
                return BadRequest("User ID is required");

            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return NotFound("User not found");

                return Ok(new UserResponseDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                return ServerError("An error occurred while retrieving the user");
            }
        }
    }
}





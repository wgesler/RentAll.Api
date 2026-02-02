using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Users;

namespace RentAll.Api.Controllers
{
	public partial class UserController
	{
		/// <summary>
		/// Update an existing user
		/// </summary>
		/// <param name="dto">User data</param>
		/// <returns>Updated user</returns>
		[HttpPut]
		public async Task<IActionResult> Update([FromBody] UpdateUserDto dto)
		{
			if (dto == null)
				return BadRequest("User data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid || !IsValidEmail(dto.Email))
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				// Check if user exists
				var existingUser = await _userRepository.GetByIdAsync(dto.UserId);
				if (existingUser == null)
					return NotFound("User not found");


				// Check if Email is being changed and if the new email already exists
				if (existingUser.Email != dto.Email)
				{
					if (await _userRepository.ExistsByEmailAsync(dto.Email))
						return Conflict("Email already exists");
				}

				var user = dto.ToModel(dto, existingUser, CurrentUser);
				var updatedUser = await _userRepository.UpdateByIdAsync(user);
				return Ok(new UserResponseDto(updatedUser));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating user: {UserId}", dto.UserId);
				return ServerError("An error occurred while updating the user");
			}
		}
	}
}









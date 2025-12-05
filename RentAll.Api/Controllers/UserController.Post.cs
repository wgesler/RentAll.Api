using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Users;

namespace RentAll.Api.Controllers
{
	public partial class UserController
	{
		/// <summary>
		/// Create a new user
		/// </summary>
		/// <param name="dto">User data</param>
		/// <returns>Created user</returns>
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
		{
			if (dto == null)
				return BadRequest(new { message = "User data is required" });

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(new { message = errorMessage });

			try
			{
				// Check if Username already exists
				if (await _userRepository.ExistsByUsernameAsync(dto.Username))
					return Conflict(new { message = "Username already exists" });

				// Check if Email already exists
				if (await _userRepository.ExistsByEmailAsync(dto.Email))
					return Conflict(new { message = "Email already exists" });

				// Hash the password
				var passwordHash = _passwordHasher.HashPassword(dto.Password);
				var user = dto.ToModel(passwordHash, CurrentUser);
				var createdUser = await _userRepository.CreateAsync(user);
				return CreatedAtAction(nameof(GetById), new { id = createdUser.UserId }, new UserResponseDto(createdUser));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating user");
				return StatusCode(500, new { message = "An error occurred while creating the user" });
			}
		}
	}
}


using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Users;
using RentAll.Domain.Enums;

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
				return BadRequest("User data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid || !IsValidEmail(dto.Email))
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				// Check if Email already exists
				if (await _userRepository.ExistsByEmailAsync(dto.Email))
					return Conflict("Email already exists");

				// Hash the password
				var passwordHash = _passwordHasher.HashPassword(dto.Password);
				var user = dto.ToModel(passwordHash, CurrentUser);

				// Handle profile file upload if provided
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						var profilePath = await _fileService.SaveLogoAsync(dto.OrganizationId, null, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
						user.ProfilePath = profilePath;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error saving user profile");
						return ServerError("An error occurred while saving the profile file");
					}
				}

				var createdUser = await _userRepository.CreateAsync(user);
				var response = new UserResponseDto(createdUser);
				if (!string.IsNullOrWhiteSpace(createdUser.ProfilePath))
				{
					response.FileDetails = await _fileService.GetFileDetailsAsync(createdUser.OrganizationId, null, createdUser.ProfilePath);
				}
				return CreatedAtAction(nameof(GetById), new { id = createdUser.UserId }, response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating user");
				return ServerError("An error occurred while creating the user");
			}
		}
	}
}









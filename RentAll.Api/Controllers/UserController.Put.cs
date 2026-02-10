using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Users;
using RentAll.Domain.Enums;

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

				// Only allow password resets here if the user is an Admin
				string? passwordHash = null;
				if (CurrentUserGroups.Contains("Admin") && dto.Password != null)
					passwordHash = _passwordHasher.HashPassword(dto.Password);
				else
					passwordHash = existingUser.PasswordHash;
				var user = dto.ToModel(dto, passwordHash, CurrentUser);

				// Handle profile file upload if provided
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						// Delete old profile if it exists
						if (!string.IsNullOrWhiteSpace(existingUser.ProfilePath))
							await _fileService.DeleteLogoAsync(existingUser.OrganizationId, null, existingUser.ProfilePath);

						// Save new profile
						var profilePath = await _fileService.SaveLogoAsync(existingUser.OrganizationId, null, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
						user.ProfilePath = profilePath;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error saving user profile");
						return ServerError("An error occurred while saving the profile file");
					}
				}
				else if (dto.ProfilePath == null)
				{
					// ProfilePath is explicitly null - delete the profile
					if (!string.IsNullOrWhiteSpace(existingUser.ProfilePath))
					{
						await _fileService.DeleteLogoAsync(existingUser.OrganizationId, null, existingUser.ProfilePath);
						user.ProfilePath = null;
					}
				}
				else
				{
					// No new file provided and ProfilePath is not null - preserve existing profile from database
					user.ProfilePath = existingUser.ProfilePath;
				}

				var updatedUser = await _userRepository.UpdateByIdAsync(user);
				var response = new UserResponseDto(updatedUser);
				if (!string.IsNullOrWhiteSpace(updatedUser.ProfilePath))
				{
					response.FileDetails = await _fileService.GetFileDetailsAsync(updatedUser.OrganizationId, null, updatedUser.ProfilePath);
				}
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating user: {UserId}", dto.UserId);
				return ServerError("An error occurred while updating the user");
			}
		}
	}
}









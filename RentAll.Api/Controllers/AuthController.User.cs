using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Users;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers;

public partial class AuthController
{
    #region Get

    [HttpGet("user")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userRepository.GetAllAsync(CurrentOrganizationId);
            var response = new List<UserResponseDto>();
            foreach (var user in users)
            {
                var dto = new UserResponseDto(user);
                if (!string.IsNullOrWhiteSpace(user.ProfilePath))
                    dto.FileDetails = await _fileService.GetFileDetailsAsync(user.OrganizationId, null, user.ProfilePath);

                response.Add(dto);
            }
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return ServerError("An error occurred while retrieving users");
        }
    }

    [HttpGet("user/{UserId}")]
    public async Task<IActionResult> GetUserById(Guid UserId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(UserId);
            if (user == null)
                return NotFound("User not found");

            var response = new UserResponseDto(user);
            if (!string.IsNullOrWhiteSpace(user.ProfilePath))
                response.FileDetails = await _fileService.GetFileDetailsAsync(user.OrganizationId, null, user.ProfilePath);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", UserId);
            return ServerError("An error occurred while retrieving the user");
        }
    }

    #endregion

    #region Post

    [HttpPost("user")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (dto == null)
            return BadRequest("User data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid || !IsValidEmail(dto.Email))
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
                return Conflict("Email already exists");

            var passwordHash = _passwordHasher.HashPassword(dto.Password);
            var user = dto.ToModel(passwordHash, CurrentUser);

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
                response.FileDetails = await _fileService.GetFileDetailsAsync(createdUser.OrganizationId, null, createdUser.ProfilePath);

            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.UserId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return ServerError("An error occurred while creating the user");
        }
    }

    #endregion

    #region Put

    [HttpPut("user")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto dto)
    {
        if (dto == null)
            return BadRequest("User data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid || !IsValidEmail(dto.Email))
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existingUser = await _userRepository.GetByIdAsync(dto.UserId);
            if (existingUser == null)
                return NotFound("User not found");

            if (existingUser.Email != dto.Email && await _userRepository.ExistsByEmailAsync(dto.Email))
                return Conflict("Email already exists");

            string? passwordHash;
            if (CurrentUserGroups.Contains("Admin") && dto.Password != null)
                passwordHash = _passwordHasher.HashPassword(dto.Password);
            else
                passwordHash = existingUser.PasswordHash;

            var user = dto.ToModel(dto, passwordHash, CurrentUser);

            if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(existingUser.ProfilePath))
                        await _fileService.DeleteLogoAsync(existingUser.OrganizationId, null, existingUser.ProfilePath);

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
                if (!string.IsNullOrWhiteSpace(existingUser.ProfilePath))
                {
                    await _fileService.DeleteLogoAsync(existingUser.OrganizationId, null, existingUser.ProfilePath);
                    user.ProfilePath = null;
                }
            }
            else
            {
                user.ProfilePath = existingUser.ProfilePath;
            }

            var updatedUser = await _userRepository.UpdateByIdAsync(user);
            var response = new UserResponseDto(updatedUser);
            if (!string.IsNullOrWhiteSpace(updatedUser.ProfilePath))
                response.FileDetails = await _fileService.GetFileDetailsAsync(updatedUser.OrganizationId, null, updatedUser.ProfilePath);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", dto.UserId);
            return ServerError("An error occurred while updating the user");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("user/{userId}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        if (userId == Guid.Empty)
            return BadRequest("User ID is required");

        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            await _userRepository.DeleteByIdAsync(userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", userId);
            return ServerError("An error occurred while deleting the user");
        }
    }

    #endregion
}

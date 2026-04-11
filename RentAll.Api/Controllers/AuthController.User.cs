using RentAll.Api.Dtos.Users;

namespace RentAll.Api.Controllers;

public partial class AuthController
{
    #region Get
    [HttpGet("user")]
    public async Task<IActionResult> GetUsersByOrganizationIdAsync()
    {
        try
        {
            var users = await _userRepository.GetUsersByOrganizationIdAsync(CurrentOrganizationId);
            var response = users.Select(u => new UserResponseDto(u)).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return ServerError("An error occurred while retrieving users");
        }
    }

    [HttpGet("user/role/{roletype}")]
    public async Task<IActionResult> GetUsersByRoleAsync(string roletype)
    {
        try
        {
            var users = await _userRepository.GetUsersByRoleTypeAsync(CurrentOrganizationId, roletype);
            var response = users.Select(u => new UserResponseDto(u)).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return ServerError("An error occurred while retrieving users");
        }
    }

    [HttpGet("user/{UserId}")]
    public async Task<IActionResult> GetUserByIdAsync(Guid UserId)
    {
        try
        {
            var user = await _userRepository.GetUserByIdAsync(UserId);
            if (user == null)
                return NotFound("User not found");

            var response = new UserResponseDto(user);
            response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(user.OrganizationId, null, user.ProfilePath, ImageType.Profiles);

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

            user.ProfilePath = await _fileAttachmentHelper.SaveImageIfPresentAsync(dto.OrganizationId, null, dto.FileDetails, ImageType.Profiles);

            var createdUser = await _userRepository.CreateAsync(user);
            var response = new UserResponseDto(createdUser);
            response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(createdUser.OrganizationId, null, createdUser.ProfilePath, ImageType.Profiles);

            return Ok(response);
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
            var existingUser = await _userRepository.GetUserByIdAsync(dto.UserId);
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
            user.ContactId = dto.ContactId ?? existingUser.ContactId;

            user.ProfilePath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(
                existingUser.OrganizationId, null, dto.FileDetails, ImageType.Profiles, existingUser.ProfilePath, dto.ProfilePath);

            var updatedUser = await _userRepository.UpdateByIdAsync(user);
            var response = new UserResponseDto(updatedUser);
            response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(updatedUser.OrganizationId, null, updatedUser.ProfilePath, ImageType.Profiles);

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
    public async Task<IActionResult> DeleteUserByIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return BadRequest("User ID is required");

        try
        {
            // Check if user exists then check/delete logo
            var existingUser = await _userRepository.GetUserByIdAsync(userId);
            if (existingUser != null && !string.IsNullOrWhiteSpace(existingUser.ProfilePath))
                await _fileService.DeleteImageAsync(existingUser.OrganizationId, null, existingUser.ProfilePath, ImageType.Profiles);

            await _userRepository.DeleteUserByIdAsync(userId);
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

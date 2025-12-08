using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Users;

namespace RentAll.Api.Controllers
{
    public partial class UserController
    {
        /// <summary>
        /// Update an existing user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="dto">User data</param>
        /// <returns>Updated user</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "User data is required" });

            var (isValid, errorMessage) = dto.IsValid(id);
            if (!isValid || !IsValidEmail(dto.Email))
                return BadRequest(new { message = errorMessage });

            try
            {
                // Check if user exists
                var existingUser = await _userRepository.GetByIdAsync(id);
                if (existingUser == null)
                    return NotFound(new { message = "User not found" });


                // Check if Email is being changed and if the new email already exists
                if (existingUser.Email != dto.Email)
                {
                    if (await _userRepository.ExistsByEmailAsync(dto.Email))
                        return Conflict(new { message = "Email already exists" });
                }

                var user = dto.ToModel(dto,existingUser, CurrentUser);
                var updatedUser = await _userRepository.UpdateByIdAsync(user);
                return Ok(new UserResponseDto(updatedUser));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the user" });
            }
        }
    }
}
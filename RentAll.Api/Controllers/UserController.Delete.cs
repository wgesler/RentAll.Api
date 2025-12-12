using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class UserController
    {
        /// <summary>
        /// Delete a user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { message = "User ID is required" });

            try
            {
                // Check if user exists
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                await _userRepository.DeleteByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the user" });
            }
        }
    }
}




using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        /// <summary>
        /// Delete an organization
        /// </summary>
        /// <param name="id">Organization ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest("OrganizationId is required");

            try
            {
                var existing = await _organizationRepository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound("Organization not found");

                var users = await _userRepository.GetAllAsync(existing.OrganizationId);
                if (users != null)
                    return BadRequest("Unable to delete an organization that still has users");

                await _organizationRepository.DeleteByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting organization: {OrganizationId}", id);
                return ServerError("An error occurred while deleting the organization");
            }
        }
    }
}






using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Organizations;

namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        /// <summary>
        /// Get all organizations
        /// </summary>
        /// <returns>List of organizations</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var orgs = await _organizationRepository.GetAllAsync();
                var response = orgs.Select(o => new OrganizationResponseDto(o));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all organizations");
                return StatusCode(500, new { message = "An error occurred while retrieving organizations" });
            }
        }

        /// <summary>
        /// Get organization by ID
        /// </summary>
        /// <param name="id">Organization ID</param>
        /// <returns>Organization</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { message = "OrganizationId is required" });

            try
            {
                var org = await _organizationRepository.GetByIdAsync(id);
                if (org == null)
                    return NotFound(new { message = "Organization not found" });

                return Ok(new OrganizationResponseDto(org));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organization by ID: {OrganizationId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the organization" });
            }
        }
    }
}





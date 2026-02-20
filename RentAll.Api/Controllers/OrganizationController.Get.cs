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
                var response = new List<OrganizationResponseDto>();
                foreach (var org in orgs)
                {
                    var dto = new OrganizationResponseDto(org);
                    if (!string.IsNullOrWhiteSpace(org.LogoPath))
                        dto.FileDetails = await _fileService.GetFileDetailsAsync(org.OrganizationId, null, org.LogoPath);

                    response.Add(dto);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all organizations");
                return ServerError("An error occurred while retrieving organizations");
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
                return BadRequest("OrganizationId is required");

            try
            {
                var org = await _organizationRepository.GetByIdAsync(id);
                if (org == null)
                    return NotFound("Organization not found");

                var response = new OrganizationResponseDto(org);
                if (!string.IsNullOrWhiteSpace(org.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(org.OrganizationId, null, org.LogoPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organization by ID: {OrganizationId}", id);
                return ServerError("An error occurred while retrieving the organization");
            }
        }
    }
}





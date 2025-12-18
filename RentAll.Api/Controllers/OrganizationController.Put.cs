using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Organizations;

namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        /// <summary>
        /// Update an existing organization
        /// </summary>
        /// <param name="id">Organization ID</param>
        /// <param name="dto">Organization data</param>
        /// <returns>Updated organization</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Organization data is required" });

            var (isValid, errorMessage) = dto.IsValid(id);
            if (!isValid)
                return BadRequest(new { message = errorMessage });

            try
            {
                var existing = await _organizationRepository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = "Organization not found" });

                // If OrganizationCode changed, ensure new one is unique
                if (!string.Equals(existing.OrganizationCode, dto.OrganizationCode, StringComparison.OrdinalIgnoreCase))
                    return Conflict(new { message = "OrganizationCode cannot change" });

                var model = dto.ToModel(CurrentUser);
                var updated = await _organizationRepository.UpdateByIdAsync(model);
                return Ok(new OrganizationResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization: {OrganizationId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the organization" });
            }
        }
    }
}



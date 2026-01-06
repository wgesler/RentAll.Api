using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Organizations;
using RentAll.Domain.Enums;

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
                return BadRequest("Organization data is required");

            var (isValid, errorMessage) = dto.IsValid(id);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existing = await _organizationRepository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound("Organization not found");

                // If OrganizationCode changed, ensure new one is unique
                if (!string.Equals(existing.OrganizationCode, dto.OrganizationCode, StringComparison.OrdinalIgnoreCase))
                    return Conflict("OrganizationCode cannot change");

                var model = dto.ToModel(CurrentUser);

				// Handle logo file upload if provided
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						// Delete old logo if it exists
						if (!string.IsNullOrWhiteSpace(existing.LogoPath))
							await _fileService.DeleteLogoAsync(existing.LogoPath);

						// Save new logo
						var logoPath = await _fileService.SaveLogoAsync(dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
						model.LogoPath = logoPath;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error saving organization logo");
						return ServerError("An error occurred while saving the logo file");
					}
				}
				else if (string.IsNullOrWhiteSpace(dto.LogoPath))
				{
					// If LogoPath is explicitly set to null/empty, delete the old logo
					if (!string.IsNullOrWhiteSpace(existing.LogoPath))
					{
						await _fileService.DeleteLogoAsync(existing.LogoPath);
						model.LogoPath = null;
					}
				}

                var updated = await _organizationRepository.UpdateByIdAsync(model);
                var response = new OrganizationResponseDto(updated);
                if (!string.IsNullOrWhiteSpace(updated.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(updated.LogoPath);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization: {OrganizationId}", id);
                return ServerError("An error occurred while updating the organization");
            }
        }
    }
}



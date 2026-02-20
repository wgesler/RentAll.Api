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
        /// <param name="dto">Organization data</param>
        /// <returns>Updated organization</returns>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateOrganizationDto dto)
        {
            if (dto == null)
                return BadRequest("Organization data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existing = await _organizationRepository.GetByIdAsync(dto.OrganizationId);
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
                            await _fileService.DeleteLogoAsync(existing.OrganizationId, null, existing.LogoPath);

                        // Save new logo
                        var logoPath = await _fileService.SaveLogoAsync(existing.OrganizationId, null, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
                        model.LogoPath = logoPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving organization logo");
                        return ServerError("An error occurred while saving the logo file");
                    }
                }
                else if (dto.LogoPath == null)
                {
                    // LogoPath is explicitly null - delete the logo
                    if (!string.IsNullOrWhiteSpace(existing.LogoPath))
                    {
                        await _fileService.DeleteLogoAsync(existing.OrganizationId, null, existing.LogoPath);
                        model.LogoPath = null;
                    }
                }
                else
                {
                    // No new file provided and LogoPath is not null - preserve existing logo from database
                    model.LogoPath = existing.LogoPath;
                }

                var updated = await _organizationRepository.UpdateByIdAsync(model);
                var response = new OrganizationResponseDto(updated);
                if (!string.IsNullOrWhiteSpace(updated.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(updated.OrganizationId, null, updated.LogoPath);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization: {OrganizationId}", dto.OrganizationId);
                return ServerError("An error occurred while updating the organization");
            }
        }
    }
}



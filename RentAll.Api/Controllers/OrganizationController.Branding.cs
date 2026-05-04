
namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        #region Get

        [HttpGet("branding")]
        public async Task<IActionResult> GetBranding()
        {
            try
            {
                var branding = await _organizationRepository.GetBrandingByOrganizationIdAsync(CurrentOrganizationId);
                if (branding == null)
                    return NotFound("Branding not found");

                var response = new BrandingResponseDto(branding);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                    branding.OrganizationId, null, branding.LogoPath, ImageType.Logos);
                response.CollapsedFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                    branding.OrganizationId, null, branding.CollapsedLogoPath, ImageType.Logos);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organization branding");
                return ServerError("An error occurred while retrieving branding");
            }
        }

        #endregion

        #region Put

        [HttpPut("branding")]
        public async Task<IActionResult> UpdateBranding([FromBody] UpdateBrandingDto dto)
        {
            if (dto == null)
                return BadRequest("Branding data is required");

            if (dto.OrganizationId != CurrentOrganizationId)
                return BadRequest("OrganizationId does not match the current organization");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingBranding = await _organizationRepository.GetBrandingByOrganizationIdAsync(CurrentOrganizationId);
                if (existingBranding == null)
                    return NotFound("Branding not found");

                var model = dto.ToModel();
                var normalizedDtoLogoPath = string.IsNullOrWhiteSpace(dto.LogoPath) ? null : dto.LogoPath;
                var normalizedDtoCollapsedLogoPath = string.IsNullOrWhiteSpace(dto.CollapsedLogoPath) ? null : dto.CollapsedLogoPath;
                model.LogoPath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(
                    CurrentOrganizationId, null, dto.FileDetails, ImageType.Logos, existingBranding.LogoPath, normalizedDtoLogoPath);
                model.CollapsedLogoPath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(
                    CurrentOrganizationId, null, dto.CollapsedFileDetails, ImageType.Logos, existingBranding.CollapsedLogoPath, normalizedDtoCollapsedLogoPath);

                var updated = await _organizationRepository.UpsertBrandingByOrganizationIdAsync(model, CurrentUser);
                var response = new BrandingResponseDto(updated);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                    updated.OrganizationId, null, updated.LogoPath, ImageType.Logos);
                response.CollapsedFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                    updated.OrganizationId, null, updated.CollapsedLogoPath, ImageType.Logos);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization branding: {OrganizationId}", dto.OrganizationId);
                return ServerError("An error occurred while updating branding");
            }
        }

        #endregion
    }
}

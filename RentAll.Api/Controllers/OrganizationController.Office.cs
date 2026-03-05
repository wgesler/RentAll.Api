
namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {

        #region Get
        [HttpGet("office/{organizationId:guid}")]
        public async Task<IActionResult> GetAllOffices(Guid organizationId)
        {
            try
            {
                IEnumerable<Office> offices;
                if (IsSuperAdmin())
                    offices = await _organizationRepository.GetOfficesByOrganizationIdAsync(organizationId);
                else if (IsAdmin())
                    offices = await _organizationRepository.GetOfficesByOrganizationIdAsync(CurrentOrganizationId);
                else
                    offices = await _organizationRepository.GetOfficesByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);

                var response = new List<OfficeResponseDto>();
                foreach (var office in offices)
                {
                    var dto = new OfficeResponseDto(office);
                    if (!string.IsNullOrWhiteSpace(office.LogoPath))
                        dto.FileDetails = await _fileService.GetFileDetailsAsync(office.OrganizationId, null, office.LogoPath);

                    response.Add(dto);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all offices");
                return ServerError("An error occurred while retrieving offices");
            }
        }

        [HttpGet("office/{officeId}")]
        public async Task<IActionResult> GetOfficeById(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                var office = await _organizationRepository.GetOfficeByIdAsync(officeId, CurrentOrganizationId);
                if (office == null)
                    return NotFound("Office not found");

                var response = new OfficeResponseDto(office);
                if (!string.IsNullOrWhiteSpace(office.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(office.OrganizationId, null, office.LogoPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting office by ID: {OfficeId}", officeId);
                return ServerError("An error occurred while retrieving the office");
            }
        }
        #endregion

        #region Post
        [HttpPost("office")]
        public async Task<IActionResult> CreateOffice([FromBody] OfficeCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Office data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                if (await _organizationRepository.ExistsByOfficeCodeAsync(dto.OfficeCode, CurrentOrganizationId))
                    return Conflict("Office Code already exists");

                var office = dto.ToModel();

                // Handle logo file upload if provided
                if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                {
                    try
                    {
                        var logoPath = await _fileService.SaveLogoAsync(CurrentOrganizationId, null, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
                        office.LogoPath = logoPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving office logo");
                        return ServerError("An error occurred while saving the logo file");
                    }
                }

                var createdOffice = await _organizationRepository.CreateAsync(office);

                var response = new OfficeResponseDto(createdOffice);
                if (!string.IsNullOrWhiteSpace(createdOffice.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(createdOffice.OrganizationId, null, createdOffice.LogoPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating office");
                return ServerError("An error occurred while creating the office");
            }
        }
        #endregion

        #region Put
        [HttpPut("office")]
        public async Task<IActionResult> UpdateOffice([FromBody] OfficeUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Office data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingOffice = await _organizationRepository.GetOfficeByIdAsync(dto.OfficeId, CurrentOrganizationId);
                if (existingOffice == null)
                    return NotFound("Office not found");

                if (existingOffice.OfficeCode != dto.OfficeCode)
                {
                    if (await _organizationRepository.ExistsByOfficeCodeAsync(dto.OfficeCode, CurrentOrganizationId))
                        return Conflict("Office Code already exists");
                }

                var office = dto.ToModel();

                // Handle logo file upload if provided
                if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                {
                    try
                    {
                        // Delete old logo if it exists (fail silently if delete fails)
                        if (!string.IsNullOrWhiteSpace(existingOffice.LogoPath))
                        {
                            try
                            {
                                await _fileService.DeleteImageAsync(existingOffice.OrganizationId, null, existingOffice.LogoPath, ImageType.Logos);
                            }
                            catch (Exception deleteEx)
                            {
                                _logger.LogWarning(deleteEx, "Failed to delete old logo, continuing with new logo upload: {LogoPath}", existingOffice.LogoPath);
                            }
                        }

                        // Save new logo
                        var logoPath = await _fileService.SaveLogoAsync(existingOffice.OrganizationId, null, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
                        office.LogoPath = logoPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving office logo");
                        return ServerError("An error occurred while saving the logo file");
                    }
                }
                else if (dto.LogoPath == null)
                {
                    // LogoPath is explicitly null - delete the logo
                    if (!string.IsNullOrWhiteSpace(existingOffice.LogoPath))
                    {
                        try
                        {
                            await _fileService.DeleteImageAsync(existingOffice.OrganizationId, null, existingOffice.LogoPath, ImageType.Logos);
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogWarning(deleteEx, "Failed to delete logo during update: {LogoPath}", existingOffice.LogoPath);
                        }
                        office.LogoPath = null;
                    }
                }
                else
                {
                    // No new file provided and LogoPath is not null - preserve existing logo from database
                    office.LogoPath = existingOffice.LogoPath;
                }

                var updatedOffice = await _organizationRepository.UpdateByIdAsync(office);
                var response = new OfficeResponseDto(updatedOffice);
                if (!string.IsNullOrWhiteSpace(updatedOffice.LogoPath))
                {
                    try
                    {
                        response.FileDetails = await _fileService.GetFileDetailsAsync(updatedOffice.OrganizationId, null, updatedOffice.LogoPath);
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogWarning(fileEx, "Failed to retrieve file details for logo, continuing with response: {LogoPath}", updatedOffice.LogoPath);
                        // Continue without file details if retrieval fails
                    }
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating office: {OfficeId}", dto.OfficeId);
                return ServerError("An error occurred while updating the office");
            }
        }
        #endregion

        #region Delete
        [HttpDelete("office/{officeId}")]
        public async Task<IActionResult> DeleteOfficeByIdAsync(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                // Check if office exists check/delete logo before deleting office
                var existingOffice = await _organizationRepository.GetOfficeByIdAsync(officeId, CurrentOrganizationId);
                if (existingOffice != null && !string.IsNullOrWhiteSpace(existingOffice.LogoPath))
                    await _fileService.DeleteImageAsync(existingOffice.OrganizationId, GetOfficeName(officeId), existingOffice.LogoPath, ImageType.Logos);

                await _organizationRepository.DeleteOfficeByIdAsync(officeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting office: {OfficeId}", officeId);
                return ServerError("An error occurred while deleting the office");
            }
        }
        #endregion
    }
}

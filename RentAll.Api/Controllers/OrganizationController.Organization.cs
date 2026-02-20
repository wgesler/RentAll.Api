
namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {

        #region Get
        /// <summary>
        /// Get all organizations
        /// </summary>
        /// <returns>List of organizations</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllOrganizations()
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
        /// <param name="organizationId">Organization ID</param>
        /// <returns>Organization</returns>
        [HttpGet("{organizationId}")]
        public async Task<IActionResult> GetOrganizationById(Guid organizationId)
        {
            if (organizationId == Guid.Empty)
                return BadRequest("OrganizationId is required");

            try
            {
                var org = await _organizationRepository.GetByIdAsync(organizationId);
                if (org == null)
                    return NotFound("Organization not found");

                var response = new OrganizationResponseDto(org);
                if (!string.IsNullOrWhiteSpace(org.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(org.OrganizationId, null, org.LogoPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organization by ID: {OrganizationId}", organizationId);
                return ServerError("An error occurred while retrieving the organization");
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Create a new organization
        /// </summary>
        /// <param name="dto">Organization data</param>
        /// <returns>Created organization</returns>
        [HttpPost]
        public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationDto dto)
        {
            if (dto == null)
                return BadRequest("Organization data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Generate OrganizationId first so we can use it for file storage
                var organizationId = Guid.NewGuid();

                // Get a new organization code
                var code = await _organizationManager.GenerateEntityCodeAsync();
                var model = dto.ToModel(code, CurrentUser);
                model.OrganizationId = organizationId;

                // Handle logo file upload if provided
                if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                {
                    try
                    {
                        var logoPath = await _fileService.SaveLogoAsync(organizationId, null, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
                        model.LogoPath = logoPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving organization logo");
                        return ServerError("An error occurred while saving the logo file");
                    }
                }

                var created = await _organizationRepository.CreateAsync(model);
                var response = new OrganizationResponseDto(created);
                if (!string.IsNullOrWhiteSpace(created.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(created.OrganizationId, null, created.LogoPath);
                }
                return CreatedAtAction(nameof(GetOrganizationById), new { organizationId = created.OrganizationId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization");
                return ServerError("An error occurred while creating the organization");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing organization
        /// </summary>
        /// <param name="dto">Organization data</param>
        /// <returns>Updated organization</returns>
        [HttpPut]
        public async Task<IActionResult> UpdateOrganization([FromBody] UpdateOrganizationDto dto)
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

        #endregion

        #region Delete

        /// <summary>
        /// Delete an organization
        /// </summary>
        /// <param name="organizationId">Organization ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{organizationId}")]
        public async Task<IActionResult> DeleteOrganization(Guid organizationId)
        {
            if (organizationId == Guid.Empty)
                return BadRequest("OrganizationId is required");

            try
            {
                var existing = await _organizationRepository.GetByIdAsync(organizationId);
                if (existing == null)
                    return NotFound("Organization not found");

                var users = await _userRepository.GetAllAsync(existing.OrganizationId);
                if (users != null)
                    return BadRequest("Unable to delete an organization that still has users");

                await _organizationRepository.DeleteByIdAsync(organizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting organization: {OrganizationId}", organizationId);
                return ServerError("An error occurred while deleting the organization");
            }
        }

        #endregion

    }
}


namespace RentAll.Api.Controllers
{
    public partial class CompanyController
    {
        #region Get

        /// <summary>
        /// Get all vendors
        /// </summary>
        /// <returns>List of vendors</returns>
        [HttpGet("vendor")]
        public async Task<IActionResult> GetAllVendors()
        {
            try
            {
                var vendors = await _companiesRepository.GetAllVendorsByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = new List<VendorResponseDto>();
                foreach (var vendor in vendors)
                {
                    var dto = new VendorResponseDto(vendor);
                    if (!string.IsNullOrWhiteSpace(vendor.LogoPath))
                        dto.FileDetails = await _fileService.GetFileDetailsAsync(vendor.OrganizationId, vendor.OfficeId, vendor.LogoPath);

                    response.Add(dto);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all vendors");
                return ServerError("An error occurred while retrieving vendors");
            }
        }

        /// <summary>
        /// Get vendor by ID
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <returns>Vendor</returns>
        [HttpGet("vendor/{vendorId}")]
        public async Task<IActionResult> GetVendorById(Guid vendorId)
        {
            if (vendorId == Guid.Empty)
                return BadRequest("Vendor ID is required");

            try
            {
                var vendor = await _companiesRepository.GetVendorByIdAsync(vendorId, CurrentOrganizationId);
                if (vendor == null)
                    return NotFound("Vendor not found");

                var response = new VendorResponseDto(vendor);
                if (!string.IsNullOrWhiteSpace(vendor.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(vendor.OrganizationId, vendor.OfficeId, vendor.LogoPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor by ID: {VendorId}", vendorId);
                return ServerError("An error occurred while retrieving the vendor");
            }
        }

        #endregion

        #region Post
        /// <summary>
        /// Create a new vendor
        /// </summary>
        /// <param name="dto">Vendor data</param>
        /// <returns>Created vendor</returns>
        [HttpPost("vendor")]
        public async Task<IActionResult> CreateVendor([FromBody] CreateVendorDto dto)
        {
            if (dto == null)
                return BadRequest("Vendor data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Get a new Vendor code
                var code = await _organizationManager.GenerateEntityCodeAsync(dto.OrganizationId, EntityType.Vendor);
                var vendor = dto.ToModel(code, CurrentUser);

                // Handle logo file upload if provided
                if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                {
                    try
                    {
                        var logoPath = await _fileService.SaveLogoAsync(dto.OrganizationId, dto.OfficeId, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Vendor);
                        vendor.LogoPath = logoPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving vendor logo");
                        return ServerError("An error occurred while saving the logo file");
                    }
                }

                var createdVendor = await _companiesRepository.CreateVendorAsync(vendor);
                var response = new VendorResponseDto(createdVendor);
                if (!string.IsNullOrWhiteSpace(createdVendor.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(createdVendor.OrganizationId, createdVendor.OfficeId, createdVendor.LogoPath);
                }
                return CreatedAtAction(nameof(GetVendorById), new { id = createdVendor.VendorId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor");
                return ServerError("An error occurred while creating the vendor");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing vendor
        /// </summary>
        /// <param name="dto">Vendor data</param>
        /// <returns>Updated vendor</returns>
        [HttpPut("vendor")]
        public async Task<IActionResult> UpdateVendor([FromBody] UpdateVendorDto dto)
        {
            if (dto == null)
                return BadRequest("Vendor data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if vendor exists
                var existingVendor = await _companiesRepository.GetVendorByIdAsync(dto.VendorId, CurrentOrganizationId);
                if (existingVendor == null)
                    return NotFound("Vendor not found");

                // Check if VendorCode is being changed
                if (existingVendor.VendorCode != dto.VendorCode)
                    return BadRequest("Vendor Code cannot change");

                var vendor = dto.ToModel(CurrentUser);

                // Handle logo file upload if provided
                if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                {
                    try
                    {
                        // Delete old logo if it exists
                        if (!string.IsNullOrWhiteSpace(existingVendor.LogoPath))
                            await _fileService.DeleteLogoAsync(existingVendor.OrganizationId, existingVendor.OfficeId, existingVendor.LogoPath);

                        // Save new logo
                        var logoPath = await _fileService.SaveLogoAsync(existingVendor.OrganizationId, existingVendor.OfficeId, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Vendor);
                        vendor.LogoPath = logoPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving vendor logo");
                        return ServerError("An error occurred while saving the logo file");
                    }
                }
                else if (dto.LogoPath == null)
                {
                    // LogoPath is explicitly null - delete the logo
                    if (!string.IsNullOrWhiteSpace(existingVendor.LogoPath))
                    {
                        await _fileService.DeleteLogoAsync(existingVendor.OrganizationId, existingVendor.OfficeId, existingVendor.LogoPath);
                        vendor.LogoPath = null;
                    }
                }
                else
                {
                    // No new file provided and LogoPath is not null - preserve existing logo from database
                    vendor.LogoPath = existingVendor.LogoPath;
                }

                var updatedVendor = await _companiesRepository.UpdateVendorByIdAsync(vendor);
                var response = new VendorResponseDto(updatedVendor);
                if (!string.IsNullOrWhiteSpace(updatedVendor.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(updatedVendor.OrganizationId, updatedVendor.OfficeId, updatedVendor.LogoPath);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor: {VendorId}", dto.VendorId);
                return ServerError("An error occurred while updating the vendor");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete a vendor
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <returns>No content</returns>
        [HttpDelete("vendor/{vendorId}")]
        public async Task<IActionResult> DeleteVendor(Guid vendorId)
        {
            if (vendorId == Guid.Empty)
                return BadRequest("Vendor ID is required");

            try
            {
                // Check if vendor exists
                var vendor = await _companiesRepository.GetVendorByIdAsync(vendorId, CurrentOrganizationId);
                if (vendor == null)
                    return NotFound("Vendor not found");

                await _companiesRepository.DeleteVendorByIdAsync(vendorId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vendor: {VendorId}", vendorId);
                return ServerError("An error occurred while deleting the vendor");
            }
        }

        #endregion

    }
}

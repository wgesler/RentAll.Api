
namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {

        #region Get

        /// <summary>
        /// Get all accounting offices
        /// </summary>
        /// <returns>List of accounting offices</returns>
        [HttpGet("accounting-office")]
        public async Task<IActionResult> GetAllAccountingOffices()
        {
            try
            {
                var accountingOffices = await _organizationRepository.GetAllAccountingByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = new List<AccountingOfficeResponseDto>();
                foreach (var accountingOffice in accountingOffices)
                {
                    var dto = new AccountingOfficeResponseDto(accountingOffice);
                    if (!string.IsNullOrWhiteSpace(accountingOffice.LogoPath))
                        dto.FileDetails = await _fileService.GetFileDetailsAsync(accountingOffice.OrganizationId, accountingOffice.OfficeId, accountingOffice.LogoPath);

                    response.Add(dto);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all accounting offices");
                return ServerError("An error occurred while retrieving accounting offices");
            }
        }

        /// <summary>
        /// Get accounting office by ID
        /// </summary>
        /// <param name="officeId">Office ID</param>
        /// <returns>Accounting office</returns>
        [HttpGet("accounting-office/{officeId}")]
        public async Task<IActionResult> GetAccountingOfficeById(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                var accountingOffice = await _organizationRepository.GetAccountingByIdAsync(CurrentOrganizationId, officeId);
                if (accountingOffice == null)
                    return NotFound("Accounting office not found");

                var response = new AccountingOfficeResponseDto(accountingOffice);
                if (!string.IsNullOrWhiteSpace(accountingOffice.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(accountingOffice.OrganizationId, accountingOffice.OfficeId, accountingOffice.LogoPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accounting office by ID: {OfficeId}", officeId);
                return ServerError("An error occurred while retrieving the accounting office");
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Create a new accounting office
        /// </summary>
        /// <param name="dto">Accounting office data</param>
        /// <returns>Created accounting office</returns>
        [HttpPost("accounting-office")]
        public async Task<IActionResult> CreateAccountingOffice([FromBody] CreateAccountingOfficeDto dto)
        {
            if (dto == null)
                return BadRequest("Accounting office data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid accounting office data");

            try
            {
                // Check if accounting office already exists
                var existing = await _organizationRepository.GetAccountingByIdAsync(dto.OrganizationId, dto.OfficeId);
                if (existing != null)
                    return Conflict("Accounting office already exists");

                var accountingOffice = dto.ToModel(CurrentUser);
                accountingOffice.OrganizationId = CurrentOrganizationId;

                // Handle logo file upload if provided
                if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                {
                    try
                    {
                        var logoPath = await _fileService.SaveLogoAsync(CurrentOrganizationId, dto.OfficeId, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
                        accountingOffice.LogoPath = logoPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving accounting office logo");
                        return ServerError("An error occurred while saving the logo file");
                    }
                }

                var createdAccountingOffice = await _organizationRepository.CreateAccountingAsync(accountingOffice);

                var response = new AccountingOfficeResponseDto(createdAccountingOffice);
                if (!string.IsNullOrWhiteSpace(createdAccountingOffice.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(createdAccountingOffice.OrganizationId, createdAccountingOffice.OfficeId, createdAccountingOffice.LogoPath);

                return CreatedAtAction(nameof(GetAccountingOfficeById), new { officeId = createdAccountingOffice.OfficeId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating accounting office");
                return ServerError("An error occurred while creating the accounting office");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing accounting office
        /// </summary>
        /// <param name="dto">Accounting office data</param>
        /// <returns>Updated accounting office</returns>
        [HttpPut("accounting-office")]
        public async Task<IActionResult> UpdateAccountingOffice([FromBody] UpdateAccountingOfficeDto dto)
        {
            if (dto == null)
                return BadRequest("Accounting office data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid accounting office data");

            try
            {
                // Check if accounting office exists
                var existingAccountingOffice = await _organizationRepository.GetAccountingByIdAsync(CurrentOrganizationId, dto.OfficeId);
                if (existingAccountingOffice == null)
                    return NotFound("Accounting office not found");

                var accountingOffice = dto.ToModel(CurrentUser);
                accountingOffice.OrganizationId = CurrentOrganizationId;

                // Handle logo file upload if provided
                if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                {
                    try
                    {
                        // Delete old logo if it exists
                        if (!string.IsNullOrWhiteSpace(existingAccountingOffice.LogoPath))
                            await _fileService.DeleteLogoAsync(existingAccountingOffice.OrganizationId, existingAccountingOffice.OfficeId, existingAccountingOffice.LogoPath);

                        // Save new logo
                        var logoPath = await _fileService.SaveLogoAsync(existingAccountingOffice.OrganizationId, existingAccountingOffice.OfficeId, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
                        accountingOffice.LogoPath = logoPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving accounting office logo");
                        return ServerError("An error occurred while saving the logo file");
                    }
                }
                else if (dto.LogoPath == null)
                {
                    // LogoPath is explicitly null - delete the logo
                    if (!string.IsNullOrWhiteSpace(existingAccountingOffice.LogoPath))
                    {
                        await _fileService.DeleteLogoAsync(existingAccountingOffice.OrganizationId, existingAccountingOffice.OfficeId, existingAccountingOffice.LogoPath);
                        accountingOffice.LogoPath = null;
                    }
                }
                else
                {
                    // No new file provided and LogoPath is not null - preserve existing logo from database
                    accountingOffice.LogoPath = existingAccountingOffice.LogoPath;
                }

                var updatedAccountingOffice = await _organizationRepository.UpdateAccountingAsync(accountingOffice);
                var response = new AccountingOfficeResponseDto(updatedAccountingOffice);
                if (!string.IsNullOrWhiteSpace(updatedAccountingOffice.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(updatedAccountingOffice.OrganizationId, updatedAccountingOffice.OfficeId, updatedAccountingOffice.LogoPath);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating accounting office: {OfficeId}", dto.OfficeId);
                return ServerError("An error occurred while updating the accounting office");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete an accounting office
        /// </summary>
        /// <param name="officeId">Office ID</param>
        /// <returns>No content</returns>
        [HttpDelete("accounting-office/{officeId}")]
        public async Task<IActionResult> DeleteAccountingOffice(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                // Check if accounting office exists
                var existingAccountingOffice = await _organizationRepository.GetAccountingByIdAsync(CurrentOrganizationId, officeId);
                if (existingAccountingOffice == null)
                    return NotFound("Accounting office not found");

                // Delete logo if it exists
                if (!string.IsNullOrWhiteSpace(existingAccountingOffice.LogoPath))
                {
                    await _fileService.DeleteLogoAsync(existingAccountingOffice.OrganizationId, existingAccountingOffice.OfficeId, existingAccountingOffice.LogoPath);
                }

                await _organizationRepository.DeleteAccountingAsync(CurrentOrganizationId, officeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting accounting office: {OfficeId}", officeId);
                return ServerError("An error occurred while deleting the accounting office");
            }
        }

        #endregion

    }
}

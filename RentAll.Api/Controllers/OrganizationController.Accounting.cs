
namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {

        #region Get

        [HttpGet("accounting-office")]
        public async Task<IActionResult> GetAccountingOfficesByOfficeIdAsync()
        {
            try
            {
                var accountingOffices = await _organizationRepository.GetAccountingOfficesByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = new List<AccountingOfficeResponseDto>();
                foreach (var accountingOffice in accountingOffices)
                {
                    var dto = new AccountingOfficeResponseDto(accountingOffice);
                    if (!string.IsNullOrWhiteSpace(accountingOffice.LogoPath))
                        dto.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(accountingOffice.OrganizationId, await GetOfficeNameAsync(accountingOffice.OfficeId), accountingOffice.LogoPath, ImageType.Logos);
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

        [HttpGet("accounting-office/{officeId}")]
        public async Task<IActionResult> GetAccountingOfficeByIdAsync(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
                if (accountingOffice == null)
                    return NotFound("Accounting office not found");

                var response = new AccountingOfficeResponseDto(accountingOffice);
                if (!string.IsNullOrWhiteSpace(accountingOffice.LogoPath))
                    response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(accountingOffice.OrganizationId, await GetOfficeNameAsync(officeId), accountingOffice.LogoPath, ImageType.Logos);

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
                var existing = await _organizationRepository.GetAccountingOfficeByIdAsync(dto.OrganizationId, dto.OfficeId);
                if (existing != null)
                    return Conflict("Accounting office code already exists");

                var accountingOffice = dto.ToModel(CurrentUser);
                accountingOffice.OrganizationId = CurrentOrganizationId;

                accountingOffice.LogoPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(CurrentOrganizationId, await GetOfficeNameAsync(dto.OfficeId), dto.FileDetails, ImageType.Logos);

                var created = await _organizationRepository.CreateAccountingAsync(accountingOffice);
                var response = new AccountingOfficeResponseDto(created);
                if (!string.IsNullOrWhiteSpace(created.LogoPath))
                    response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(created.OrganizationId, await GetOfficeNameAsync(created.OfficeId), created.LogoPath, ImageType.Logos);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating accounting office");
                return ServerError("An error occurred while creating the accounting office");
            }
        }

        #endregion

        #region Put
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
                var existing = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, dto.OfficeId);
                if (existing == null)
                    return NotFound("Accounting office not found");

                var accountingOffice = dto.ToModel(CurrentUser);
                var officeName = await GetOfficeNameAsync(dto.OfficeId);
                accountingOffice.LogoPath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(dto.OrganizationId, officeName, dto.FileDetails,
                    ImageType.Logos, existing.LogoPath, dto.LogoPath);

                var updated = await _organizationRepository.UpdateAccountingAsync(accountingOffice);
                var response = new AccountingOfficeResponseDto(updated);
                if (!string.IsNullOrWhiteSpace(updated.LogoPath))
                    response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(updated.OrganizationId, officeName, updated.LogoPath, ImageType.Logos);
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
        [HttpDelete("accounting-office/{officeId}")]
        public async Task<IActionResult> DeleteAccountingOfficeByIdAsync(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                // Check if accounting office exists and be sure to delete the logo file
                var office = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
                if (office != null && !string.IsNullOrWhiteSpace(office.LogoPath))
                    await _fileService.DeleteImageAsync(office.OrganizationId, await GetOfficeNameAsync(officeId), office.LogoPath, ImageType.Logos);

                await _organizationRepository.DeleteAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
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

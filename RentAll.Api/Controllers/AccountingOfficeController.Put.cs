using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Organizations.Accounting;
using RentAll.Api.Dtos.Accounting.Invoices;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
    public partial class AccountingOfficeController
    {
        /// <summary>
        /// Update an existing accounting office
        /// </summary>
        /// <param name="dto">Accounting office data</param>
        /// <returns>Updated accounting office</returns>
        [HttpPut()]
        public async Task<IActionResult> Update([FromBody] UpdateAccountingOfficeDto dto)
        {
            if (dto == null)
                return BadRequest("Accounting office data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid accounting office data");

            try
            {
                // Check if accounting office exists
                var existingAccountingOffice = await _officeRepository.GetAccountingByIdAsync(CurrentOrganizationId, dto.OfficeId);
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

                var updatedAccountingOffice = await _officeRepository.UpdateAccountingAsync(accountingOffice);
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
    }
}

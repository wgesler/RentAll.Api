using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.AccountingOffices;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
    public partial class AccountingOfficeController
    {
        /// <summary>
        /// Create a new accounting office
        /// </summary>
        /// <param name="dto">Accounting office data</param>
        /// <returns>Created accounting office</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAccountingOfficeDto dto)
        {
            if (dto == null)
                return BadRequest("Accounting office data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid accounting office data");

            try
            {
                // Check if accounting office already exists
                var existing = await _officeRepository.GetAccountingByIdAsync(dto.OrganizationId, dto.OfficeId);
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

                var createdAccountingOffice = await _officeRepository.CreateAccountingAsync(accountingOffice);

                var response = new AccountingOfficeResponseDto(createdAccountingOffice);
                if (!string.IsNullOrWhiteSpace(createdAccountingOffice.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(createdAccountingOffice.OrganizationId, createdAccountingOffice.OfficeId, createdAccountingOffice.LogoPath);

                return CreatedAtAction(nameof(GetById), new { officeId = createdAccountingOffice.OfficeId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating accounting office");
                return ServerError("An error occurred while creating the accounting office");
            }
        }
    }
}

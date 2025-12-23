using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Companies;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
    public partial class CompanyController
    {
        /// <summary>
        /// Update an existing company
        /// </summary>
        /// <param name="id">Company ID</param>
        /// <param name="dto">Company data</param>
        /// <returns>Updated company</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Company data is required" });

            var (isValid, errorMessage) = dto.IsValid(id);
            if (!isValid)
                return BadRequest(new { message = errorMessage });

            try
            {
                // Check if company exists
                var existingCompany = await _companyRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (existingCompany == null)
                    return NotFound(new { message = "Company not found" });

                // Check if CompanyCode is being changed
                if (existingCompany.CompanyCode != dto.CompanyCode)
					return BadRequest(new { message = "Company Code cannot change" });

				var company = dto.ToModel(CurrentUser);

				// Handle logo file upload if provided
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						// Delete old logo if it exists
						if (!string.IsNullOrWhiteSpace(existingCompany.LogoPath))
							await _fileService.DeleteLogoAsync(existingCompany.LogoPath);

						// Save new logo
						var logoPath = await _fileService.SaveLogoAsync(dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Company);
						company.LogoPath = logoPath;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error saving company logo");
						return StatusCode(500, new { message = "An error occurred while saving the logo file" });
					}
				}
				else if (string.IsNullOrWhiteSpace(dto.LogoPath))
				{
					// If LogoPath is explicitly set to null/empty, delete the old logo
					if (!string.IsNullOrWhiteSpace(existingCompany.LogoPath))
					{
						await _fileService.DeleteLogoAsync(existingCompany.LogoPath);
						company.LogoPath = null;
					}
				}

                var updatedCompany = await _companyRepository.UpdateByIdAsync(company);
                return Ok(new CompanyResponseDto(updatedCompany));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company: {CompanyId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the company" });
            }
        }
    }
}








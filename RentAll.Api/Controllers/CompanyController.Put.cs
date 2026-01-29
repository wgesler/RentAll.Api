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
                return BadRequest("Company data is required");

            var (isValid, errorMessage) = dto.IsValid(id);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if company exists
                var existingCompany = await _companyRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (existingCompany == null)
                    return NotFound("Company not found");

                // Check if CompanyCode is being changed
                if (existingCompany.CompanyCode != dto.CompanyCode)
					return BadRequest("Company Code cannot change");

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
						return ServerError("An error occurred while saving the logo file");
					}
				}
				else if (dto.LogoPath == null)
				{
					// LogoPath is explicitly null - delete the logo
					if (!string.IsNullOrWhiteSpace(existingCompany.LogoPath))
					{
						await _fileService.DeleteLogoAsync(existingCompany.LogoPath);
						company.LogoPath = null;
					}
				}
				else
				{
					// No new file provided and LogoPath is not null - preserve existing logo from database
					company.LogoPath = existingCompany.LogoPath;
				}

                var updatedCompany = await _companyRepository.UpdateByIdAsync(company);
                var response = new CompanyResponseDto(updatedCompany);
                if (!string.IsNullOrWhiteSpace(updatedCompany.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(updatedCompany.LogoPath);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company: {CompanyId}", id);
                return ServerError("An error occurred while updating the company");
            }
        }
    }
}








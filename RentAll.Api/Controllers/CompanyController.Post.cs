using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Companies;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
    public partial class CompanyController
    {
        /// <summary>
        /// Create a new company
        /// </summary>
        /// <param name="dto">Company data</param>
        /// <returns>Created company</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCompanyDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Company data is required" });

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(new { message = errorMessage });

            try
            {
				// Get a new Contact code
				var code = await _organizationManager.GenerateEntityCodeAsync(dto.OrganizationId, EntityType.Company);
				var company = dto.ToModel(code, CurrentUser);

				// Handle logo file upload if provided
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						var logoPath = await _fileService.SaveLogoAsync(dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Company);
						company.LogoPath = logoPath;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error saving company logo");
						return StatusCode(500, new { message = "An error occurred while saving the logo file" });
					}
				}

                var createdCompany = await _companyRepository.CreateAsync(company);
                var response = new CompanyResponseDto(createdCompany);
                if (!string.IsNullOrWhiteSpace(createdCompany.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(createdCompany.LogoPath);
                }
                return CreatedAtAction(nameof(GetById), new { id = createdCompany.CompanyId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return StatusCode(500, new { message = "An error occurred while creating the company" });
            }
        }
    }
}








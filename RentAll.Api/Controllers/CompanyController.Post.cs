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

                var createdCompany = await _companyRepository.CreateAsync(company);
                return CreatedAtAction(nameof(GetById), new { id = createdCompany.CompanyId }, new CompanyResponseDto(createdCompany));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return StatusCode(500, new { message = "An error occurred while creating the company" });
            }
        }
    }
}






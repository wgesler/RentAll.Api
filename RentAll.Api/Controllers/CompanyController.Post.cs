using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Companies;

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
                // Check if CompanyCode already exists
                if (await _companyRepository.ExistsByCompanyCodeAsync(dto.CompanyCode))
                    return Conflict(new { message = "Company Code already exists" });

                var company = dto.ToModel(CurrentUser);
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
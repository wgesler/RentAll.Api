using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Companies;

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
                var existingCompany = await _companyRepository.GetByIdAsync(id);
                if (existingCompany == null)
                    return NotFound(new { message = "Company not found" });

                // Check if CompanyCode is being changed and if the new code already exists
                if (existingCompany.CompanyCode != dto.CompanyCode)
                {
                    if (await _companyRepository.ExistsByCompanyCodeAsync(dto.CompanyCode))
                        return Conflict(new { message = "Company Code already exists" });
                }

                var company = dto.ToModel(dto, CurrentUser);
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



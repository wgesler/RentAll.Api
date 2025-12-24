using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Companies;

namespace RentAll.Api.Controllers
{
    public partial class CompanyController
    {
		/// <summary>
		/// Get all companies
		/// </summary>
		/// <returns>List of companies</returns>
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var companies = await _companyRepository.GetAllAsync(CurrentOrganizationId);
				var response = new List<CompanyResponseDto>();
				foreach (var company in companies)
				{
					var dto = new CompanyResponseDto(company);
					if (!string.IsNullOrWhiteSpace(company.LogoPath))
						dto.FileDetails = await _fileService.GetFileDetailsAsync(company.LogoPath);

					response.Add(dto);
				}
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all companies");
				return StatusCode(500, new { message = "An error occurred while retrieving companies" });
			}
		}
		
		/// <summary>
		/// Get company by ID
		/// </summary>
		/// <param name="id">Company ID</param>
		/// <returns>Company</returns>
		[HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { message = "Company ID is required" });

            try
            {
                var company = await _companyRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (company == null)
                    return NotFound(new { message = "Company not found" });

                var response = new CompanyResponseDto(company);
                if (!string.IsNullOrWhiteSpace(company.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(company.LogoPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company by ID: {CompanyId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the company" });
            }
        }
    }
}








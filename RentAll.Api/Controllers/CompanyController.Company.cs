
namespace RentAll.Api.Controllers
{
    public partial class CompanyController
    {

        #region Get

        /// <summary>
        /// Get all companies
        /// </summary>
        /// <returns>List of companies</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllCompanies()
        {
            try
            {
                var companies = await _companiesRepository.GetAllByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = new List<CompanyResponseDto>();
                foreach (var company in companies)
                {
                    var dto = new CompanyResponseDto(company);
                    if (!string.IsNullOrWhiteSpace(company.LogoPath))
                        dto.FileDetails = await _fileService.GetFileDetailsAsync(company.OrganizationId, company.OfficeId, company.LogoPath);

                    response.Add(dto);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all companies");
                return ServerError("An error occurred while retrieving companies");
            }
        }

        /// <summary>
        /// Get company by ID
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <returns>Company</returns>
        [HttpGet("{companyId}")]
        public async Task<IActionResult> GetCompanyById(Guid companyId)
        {
            if (companyId == Guid.Empty)
                return BadRequest("Company ID is required");

            try
            {
                var company = await _companiesRepository.GetByIdAsync(companyId, CurrentOrganizationId);
                if (company == null)
                    return NotFound("Company not found");

                var response = new CompanyResponseDto(company);
                if (!string.IsNullOrWhiteSpace(company.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(company.OrganizationId, company.OfficeId, company.LogoPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company by ID: {CompanyId}", companyId);
                return ServerError("An error occurred while retrieving the company");
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Create a new company
        /// </summary>
        /// <param name="dto">Company data</param>
        /// <returns>Created company</returns>
        [HttpPost]
        public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyDto dto)
        {
            if (dto == null)
                return BadRequest("Company data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

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
                        var logoPath = await _fileService.SaveLogoAsync(dto.OrganizationId, dto.OfficeId, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Company);
                        company.LogoPath = logoPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving company logo");
                        return ServerError("An error occurred while saving the logo file");
                    }
                }

                var createdCompany = await _companiesRepository.CreateAsync(company);
                var response = new CompanyResponseDto(createdCompany);
                if (!string.IsNullOrWhiteSpace(createdCompany.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(createdCompany.OrganizationId, createdCompany.OfficeId, createdCompany.LogoPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return ServerError("An error occurred while creating the company");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing company
        /// </summary>
        /// <param name="dto">Company data</param>
        /// <returns>Updated company</returns>
        [HttpPut]
        public async Task<IActionResult> UpdateCompany([FromBody] UpdateCompanyDto dto)
        {
            if (dto == null)
                return BadRequest("Company data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if company exists
                var existingCompany = await _companiesRepository.GetByIdAsync(dto.CompanyId, CurrentOrganizationId);
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
                            await _fileService.DeleteLogoAsync(existingCompany.OrganizationId, existingCompany.OfficeId, existingCompany.LogoPath);

                        // Save new logo
                        var logoPath = await _fileService.SaveLogoAsync(existingCompany.OrganizationId, existingCompany.OfficeId, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Company);
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
                        await _fileService.DeleteLogoAsync(existingCompany.OrganizationId, existingCompany.OfficeId, existingCompany.LogoPath);
                        company.LogoPath = null;
                    }
                }
                else
                {
                    // No new file provided and LogoPath is not null - preserve existing logo from database
                    company.LogoPath = existingCompany.LogoPath;
                }

                var updatedCompany = await _companiesRepository.UpdateByIdAsync(company);
                var response = new CompanyResponseDto(updatedCompany);
                if (!string.IsNullOrWhiteSpace(updatedCompany.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(updatedCompany.OrganizationId, updatedCompany.OfficeId, updatedCompany.LogoPath);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company: {CompanyId}", dto.CompanyId);
                return ServerError("An error occurred while updating the company");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete a company
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{companyId}")]
        public async Task<IActionResult> DeleteCompany(Guid companyId)
        {
            if (companyId == Guid.Empty)
                return BadRequest("Company ID is required");

            try
            {
                // Check if company exists
                var company = await _companiesRepository.GetByIdAsync(companyId, CurrentOrganizationId);
                if (company == null)
                    return NotFound("Company not found");

                await _companiesRepository.DeleteByIdAsync(companyId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company: {CompanyId}", companyId);
                return ServerError("An error occurred while deleting the company");
            }
        }

        #endregion

    }
}

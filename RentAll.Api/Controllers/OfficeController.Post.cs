using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.OfficeConfigurations;
using RentAll.Api.Dtos.Offices;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
    public partial class OfficeController
    {
        /// <summary>
        /// Create a new office
        /// </summary>
        /// <param name="dto">Office data</param>
        /// <returns>Created office</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OfficeCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Office data is required" });

            if (string.IsNullOrWhiteSpace(dto.OfficeCode))
                return BadRequest(new { message = "Office Code is required" });

            try
            {
                if (await _officeRepository.ExistsByOfficeCodeAsync(dto.OfficeCode, CurrentOrganizationId))
                    return Conflict(new { message = "Office Code already exists" });

				var office = dto.ToModel();

				// Handle logo file upload if provided
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						var logoPath = await _fileService.SaveLogoAsync(dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
						office.LogoPath = logoPath;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error saving office logo");
						return StatusCode(500, new { message = "An error occurred while saving the logo file" });
					}
				}
				
                var createdOffice = await _officeRepository.CreateAsync(office);
                var response = new OfficeResponseDto(createdOffice);
                if (!string.IsNullOrWhiteSpace(createdOffice.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(createdOffice.LogoPath);
                }
                return CreatedAtAction(nameof(GetById), new { officeId = createdOffice.OfficeId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating office");
                return StatusCode(500, new { message = "An error occurred while creating the office" });
            }
        }

		/// <summary>
		/// Create a new office configuration
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <param name="dto">Office configuration data</param>
		/// <returns>Created office configuration</returns>
		[HttpPost("{officeId}/configuration")]
		public async Task<IActionResult> CreateConfiguration(int officeId, [FromBody] OfficeConfigurationCreateDto dto)
		{
			if (dto == null)
				return BadRequest(new { message = "Office configuration data is required" });

			if (officeId != dto.OfficeId)
				return BadRequest(new { message = "Office ID mismatch" });

			try
			{
				// Verify office exists and belongs to organization
				var office = await _officeRepository.GetByIdAsync(officeId, CurrentOrganizationId);
				if (office == null)
					return NotFound(new { message = "Office not found" });

				// Check if configuration already exists
				var existingConfig = await _officeConfigurationRepository.GetByOfficeIdAsync(officeId);
				if (existingConfig != null)
					return Conflict(new { message = "Office configuration already exists for this office" });

				var configuration = dto.ToModel();
				var createdConfiguration = await _officeConfigurationRepository.CreateAsync(configuration);
				return CreatedAtAction(nameof(GetConfiguration), new { officeId = createdConfiguration.OfficeId }, new OfficeConfigurationResponseDto(createdConfiguration));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating office configuration: {OfficeId}", officeId);
				return StatusCode(500, new { message = "An error occurred while creating the office configuration" });
			}
		}
	}
}


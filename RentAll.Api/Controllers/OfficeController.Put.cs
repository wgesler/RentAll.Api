using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.OfficeConfigurations;
using RentAll.Api.Dtos.Offices;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
	public partial class OfficeController
	{
		/// <summary>
		/// Update an existing office
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <param name="dto">Office data</param>
		/// <returns>Updated office</returns>
		[HttpPut("{officeId}")]
		public async Task<IActionResult> Update(int officeId, [FromBody] OfficeUpdateDto dto)
		{
		if (dto == null)
			return BadRequest("Office data is required");

		if (officeId != dto.OfficeId)
			return BadRequest("Office ID mismatch");

		if (string.IsNullOrWhiteSpace(dto.OfficeCode))
			return BadRequest("Office Code is required");

			try
			{
				var existingOffice = await _officeRepository.GetByIdAsync(officeId, CurrentOrganizationId);
			if (existingOffice == null)
				return NotFound("Office not found");

				if (existingOffice.OfficeCode != dto.OfficeCode)
				{
					if (await _officeRepository.ExistsByOfficeCodeAsync(dto.OfficeCode, CurrentOrganizationId))
						return Conflict("Office Code already exists");
				}

				var office = dto.ToModel();

				// Handle logo file upload if provided
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						// Delete old logo if it exists
						if (!string.IsNullOrWhiteSpace(existingOffice.LogoPath))
							await _fileService.DeleteLogoAsync(existingOffice.LogoPath);

						// Save new logo
						var logoPath = await _fileService.SaveLogoAsync(dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
						office.LogoPath = logoPath;
					}
					catch (Exception ex)
					{
					_logger.LogError(ex, "Error saving office logo");
					return ServerError("An error occurred while saving the logo file");
					}
				}
				else if (string.IsNullOrWhiteSpace(dto.LogoPath))
				{
					// If LogoPath is explicitly set to null/empty, delete the old logo
					if (!string.IsNullOrWhiteSpace(existingOffice.LogoPath))
					{
						await _fileService.DeleteLogoAsync(existingOffice.LogoPath);
						office.LogoPath = null;
					}
				}

				var updatedOffice = await _officeRepository.UpdateByIdAsync(office);
				var response = new OfficeResponseDto(updatedOffice);
				if (!string.IsNullOrWhiteSpace(updatedOffice.LogoPath))
				{
					response.FileDetails = await _fileService.GetFileDetailsAsync(updatedOffice.LogoPath);
				}
				return Ok(response);
			}
			catch (Exception ex)
			{
			_logger.LogError(ex, "Error updating office: {OfficeId}", officeId);
			return ServerError("An error occurred while updating the office");
			}
		}

		/// <summary>
		/// Update an existing office configuration
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <param name="dto">Office configuration data</param>
		/// <returns>Updated office configuration</returns>
		[HttpPut("{officeId}/configuration")]
		public async Task<IActionResult> UpdateConfiguration(int officeId, [FromBody] OfficeConfigurationUpdateDto dto)
		{
		if (dto == null)
			return BadRequest("Office configuration data is required");

		if (officeId != dto.OfficeId)
			return BadRequest("Office ID mismatch");

			try
			{
				// Verify office exists and belongs to organization
				var office = await _officeRepository.GetByIdAsync(officeId, CurrentOrganizationId);
			if (office == null)
				return NotFound("Office not found");

			// Verify configuration exists
			var existingConfig = await _officeConfigurationRepository.GetByOfficeIdAsync(officeId);
			if (existingConfig == null)
				return NotFound("Office configuration not found");

				var configuration = dto.ToModel();
				var updatedConfiguration = await _officeConfigurationRepository.UpdateByOfficeIdAsync(configuration);
				return Ok(new OfficeConfigurationResponseDto(updatedConfiguration));
			}
			catch (Exception ex)
			{
			_logger.LogError(ex, "Error updating office configuration: {OfficeId}", officeId);
			return ServerError("An error occurred while updating the office configuration");
			}
		}
	}
}


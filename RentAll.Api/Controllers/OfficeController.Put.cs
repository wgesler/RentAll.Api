using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Offices;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
	public partial class OfficeController
	{
		/// <summary>
		/// Update an existing office
		/// </summary>
		/// <param name="dto">Office data</param>
		/// <returns>Updated office</returns>
		[HttpPut]
		public async Task<IActionResult> Update([FromBody] OfficeUpdateDto dto)
		{
			if (dto == null)
				return BadRequest("Office data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				var existingOffice = await _officeRepository.GetByIdAsync(dto.OfficeId, CurrentOrganizationId);
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
				else if (dto.LogoPath == null)
				{
					// LogoPath is explicitly null - delete the logo
					if (!string.IsNullOrWhiteSpace(existingOffice.LogoPath))
					{
						await _fileService.DeleteLogoAsync(existingOffice.LogoPath);
						office.LogoPath = null;
					}
				}
				else
				{
					// No new file provided and LogoPath is not null - preserve existing logo from database
					office.LogoPath = existingOffice.LogoPath;
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
			_logger.LogError(ex, "Error updating office: {OfficeId}", dto.OfficeId);
			return ServerError("An error occurred while updating the office");
			}
		}
	}
}


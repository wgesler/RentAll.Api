using Microsoft.AspNetCore.Mvc;
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
				return BadRequest("Office data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				if (await _officeRepository.ExistsByOfficeCodeAsync(dto.OfficeCode, CurrentOrganizationId))
					return Conflict("Office Code already exists");

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
						return ServerError("An error occurred while saving the logo file");
					}
				}

				var createdOffice = await _officeRepository.CreateAsync(office);

				var response = new OfficeResponseDto(createdOffice);
				if (!string.IsNullOrWhiteSpace(createdOffice.LogoPath))
					response.FileDetails = await _fileService.GetFileDetailsAsync(createdOffice.LogoPath);

				return CreatedAtAction(nameof(GetById), new { officeId = createdOffice.OfficeId }, response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating office");
				return ServerError("An error occurred while creating the office");
			}
		}
	}
}


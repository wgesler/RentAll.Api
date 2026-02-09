using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Organizations;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
	public partial class OrganizationController
	{
		/// <summary>
		/// Create a new organization
		/// </summary>
		/// <param name="dto">Organization data</param>
		/// <returns>Created organization</returns>
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateOrganizationDto dto)
		{
			if (dto == null)
				return BadRequest("Organization data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				// Generate OrganizationId first so we can use it for file storage
				var organizationId = Guid.NewGuid();
				
				// Get a new organization code
				var code = await _organizationManager.GenerateEntityCodeAsync();
				var model = dto.ToModel(code, CurrentUser);
				model.OrganizationId = organizationId;

				// Handle logo file upload if provided
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						var logoPath = await _fileService.SaveLogoAsync(organizationId, null, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
						model.LogoPath = logoPath;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error saving organization logo");
						return ServerError("An error occurred while saving the logo file");
					}
				}

				var created = await _organizationRepository.CreateAsync(model);
				var response = new OrganizationResponseDto(created);
				if (!string.IsNullOrWhiteSpace(created.LogoPath))
				{
					response.FileDetails = await _fileService.GetFileDetailsAsync(created.OrganizationId, null, created.LogoPath);
				}
				return CreatedAtAction(nameof(GetById), new { id = created.OrganizationId }, response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating organization");
				return ServerError("An error occurred while creating the organization");
			}
		}
	}
}





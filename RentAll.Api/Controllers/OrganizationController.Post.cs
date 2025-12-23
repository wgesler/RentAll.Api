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
                return BadRequest(new { message = "Organization data is required" });

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(new { message = errorMessage });

            try
            {
				// Get a new organization code
				var code = await _organizationManager.GenerateEntityCodeAsync();
				var model = dto.ToModel(code, CurrentUser);

				// Handle logo file upload if provided
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						var logoPath = await _fileService.SaveLogoAsync(dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Organization);
						model.LogoPath = logoPath;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error saving organization logo");
						return StatusCode(500, new { message = "An error occurred while saving the logo file" });
					}
				}

                var created = await _organizationRepository.CreateAsync(model);
                return CreatedAtAction(nameof(GetById), new { id = created.OrganizationId }, new OrganizationResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization");
                return StatusCode(500, new { message = "An error occurred while creating the organization" });
            }
        }
    }
}





using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Vendors;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
    public partial class VendorController
    {
        /// <summary>
        /// Create a new vendor
        /// </summary>
        /// <param name="dto">Vendor data</param>
        /// <returns>Created vendor</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVendorDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Vendor data is required" });

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(new { message = errorMessage });

            try
            {
				// Get a new Vendor code
				var code = await _organizationManager.GenerateEntityCodeAsync(dto.OrganizationId, EntityType.Vendor);
				var vendor = dto.ToModel(code, CurrentUser);

				// Handle logo file upload if provided
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						var logoPath = await _fileService.SaveLogoAsync(dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Vendor);
						vendor.LogoPath = logoPath;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error saving vendor logo");
						return StatusCode(500, new { message = "An error occurred while saving the logo file" });
					}
				}

                var createdVendor = await _vendorRepository.CreateAsync(vendor);
                var response = new VendorResponseDto(createdVendor);
                if (!string.IsNullOrWhiteSpace(createdVendor.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(createdVendor.LogoPath);
                }
                return CreatedAtAction(nameof(GetById), new { id = createdVendor.VendorId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor");
                return StatusCode(500, new { message = "An error occurred while creating the vendor" });
            }
        }
    }
}




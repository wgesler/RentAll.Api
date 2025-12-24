using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Vendors;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
    public partial class VendorController
    {
        /// <summary>
        /// Update an existing vendor
        /// </summary>
        /// <param name="id">Vendor ID</param>
        /// <param name="dto">Vendor data</param>
        /// <returns>Updated vendor</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Vendor data is required" });

            var (isValid, errorMessage) = dto.IsValid(id);
            if (!isValid)
                return BadRequest(new { message = errorMessage });

            try
            {
                // Check if vendor exists
                var existingVendor = await _vendorRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (existingVendor == null)
                    return NotFound(new { message = "Vendor not found" });

                // Check if VendorCode is being changed
                if (existingVendor.VendorCode != dto.VendorCode)
					return BadRequest(new { message = "Vendor Code cannot change" });

				var vendor = dto.ToModel(CurrentUser);

				// Handle logo file upload if provided
				if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
				{
					try
					{
						// Delete old logo if it exists
						if (!string.IsNullOrWhiteSpace(existingVendor.LogoPath))
							await _fileService.DeleteLogoAsync(existingVendor.LogoPath);

						// Save new logo
						var logoPath = await _fileService.SaveLogoAsync(dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Vendor);
						vendor.LogoPath = logoPath;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error saving vendor logo");
						return StatusCode(500, new { message = "An error occurred while saving the logo file" });
					}
				}
				else if (string.IsNullOrWhiteSpace(dto.LogoPath))
				{
					// If LogoPath is explicitly set to null/empty, delete the old logo
					if (!string.IsNullOrWhiteSpace(existingVendor.LogoPath))
					{
						await _fileService.DeleteLogoAsync(existingVendor.LogoPath);
						vendor.LogoPath = null;
					}
				}

                var updatedVendor = await _vendorRepository.UpdateByIdAsync(vendor);
                var response = new VendorResponseDto(updatedVendor);
                if (!string.IsNullOrWhiteSpace(updatedVendor.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(updatedVendor.LogoPath);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor: {VendorId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the vendor" });
            }
        }
    }
}




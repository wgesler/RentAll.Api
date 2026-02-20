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
        /// <param name="dto">Vendor data</param>
        /// <returns>Updated vendor</returns>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateVendorDto dto)
        {
            if (dto == null)
                return BadRequest("Vendor data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if vendor exists
                var existingVendor = await _companiesRepository.GetVendorByIdAsync(dto.VendorId, CurrentOrganizationId);
                if (existingVendor == null)
                    return NotFound("Vendor not found");

                // Check if VendorCode is being changed
                if (existingVendor.VendorCode != dto.VendorCode)
                    return BadRequest("Vendor Code cannot change");

                var vendor = dto.ToModel(CurrentUser);

                // Handle logo file upload if provided
                if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                {
                    try
                    {
                        // Delete old logo if it exists
                        if (!string.IsNullOrWhiteSpace(existingVendor.LogoPath))
                            await _fileService.DeleteLogoAsync(existingVendor.OrganizationId, existingVendor.OfficeId, existingVendor.LogoPath);

                        // Save new logo
                        var logoPath = await _fileService.SaveLogoAsync(existingVendor.OrganizationId, existingVendor.OfficeId, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, EntityType.Vendor);
                        vendor.LogoPath = logoPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving vendor logo");
                        return ServerError("An error occurred while saving the logo file");
                    }
                }
                else if (dto.LogoPath == null)
                {
                    // LogoPath is explicitly null - delete the logo
                    if (!string.IsNullOrWhiteSpace(existingVendor.LogoPath))
                    {
                        await _fileService.DeleteLogoAsync(existingVendor.OrganizationId, existingVendor.OfficeId, existingVendor.LogoPath);
                        vendor.LogoPath = null;
                    }
                }
                else
                {
                    // No new file provided and LogoPath is not null - preserve existing logo from database
                    vendor.LogoPath = existingVendor.LogoPath;
                }

                var updatedVendor = await _companiesRepository.UpdateVendorByIdAsync(vendor);
                var response = new VendorResponseDto(updatedVendor);
                if (!string.IsNullOrWhiteSpace(updatedVendor.LogoPath))
                {
                    response.FileDetails = await _fileService.GetFileDetailsAsync(updatedVendor.OrganizationId, updatedVendor.OfficeId, updatedVendor.LogoPath);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor: {VendorId}", dto.VendorId);
                return ServerError("An error occurred while updating the vendor");
            }
        }
    }
}




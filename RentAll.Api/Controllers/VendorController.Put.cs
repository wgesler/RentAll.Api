using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Vendors;

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
                var updatedVendor = await _vendorRepository.UpdateByIdAsync(vendor);
                return Ok(new VendorResponseDto(updatedVendor));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor: {VendorId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the vendor" });
            }
        }
    }
}


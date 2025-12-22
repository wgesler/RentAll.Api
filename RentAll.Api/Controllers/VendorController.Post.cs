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

                var createdVendor = await _vendorRepository.CreateAsync(vendor);
                return CreatedAtAction(nameof(GetById), new { id = createdVendor.VendorId }, new VendorResponseDto(createdVendor));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor");
                return StatusCode(500, new { message = "An error occurred while creating the vendor" });
            }
        }
    }
}




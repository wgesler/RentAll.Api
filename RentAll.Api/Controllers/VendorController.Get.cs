using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Vendors;

namespace RentAll.Api.Controllers
{
    public partial class VendorController
    {
		/// <summary>
		/// Get all vendors
		/// </summary>
		/// <returns>List of vendors</returns>
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var vendors = await _vendorRepository.GetAllAsync(CurrentOrganizationId);
				var response = vendors.Select(v => new VendorResponseDto(v));
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all vendors");
				return StatusCode(500, new { message = "An error occurred while retrieving vendors" });
			}
		}
		
		/// <summary>
		/// Get vendor by ID
		/// </summary>
		/// <param name="id">Vendor ID</param>
		/// <returns>Vendor</returns>
		[HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { message = "Vendor ID is required" });

            try
            {
                var vendor = await _vendorRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (vendor == null)
                    return NotFound(new { message = "Vendor not found" });

                return Ok(new VendorResponseDto(vendor));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor by ID: {VendorId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the vendor" });
            }
        }
    }
}


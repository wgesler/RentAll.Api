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
				var vendors = await _vendorRepository.GetAllByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
				var response = new List<VendorResponseDto>();
				foreach (var vendor in vendors)
				{
					var dto = new VendorResponseDto(vendor);
					if (!string.IsNullOrWhiteSpace(vendor.LogoPath))
						dto.FileDetails = await _fileService.GetFileDetailsAsync(vendor.LogoPath);

					response.Add(dto);
				}
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all vendors");
				return ServerError("An error occurred while retrieving vendors");
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
                return BadRequest("Vendor ID is required");

            try
            {
                var vendor = await _vendorRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (vendor == null)
                    return NotFound("Vendor not found");

                var response = new VendorResponseDto(vendor);
                if (!string.IsNullOrWhiteSpace(vendor.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(vendor.LogoPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor by ID: {VendorId}", id);
                return ServerError("An error occurred while retrieving the vendor");
            }
        }
    }
}




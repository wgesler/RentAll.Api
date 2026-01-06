using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class VendorController
    {
        /// <summary>
        /// Delete a vendor
        /// </summary>
        /// <param name="id">Vendor ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest("Vendor ID is required");

            try
            {
                // Check if vendor exists
                var vendor = await _vendorRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (vendor == null)
                    return NotFound("Vendor not found");

                await _vendorRepository.DeleteByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vendor: {VendorId}", id);
                return ServerError("An error occurred while deleting the vendor");
            }
        }
    }
}





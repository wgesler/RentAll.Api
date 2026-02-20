using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class RegionController
    {
        /// <summary>
        /// Delete a region
        /// </summary>
        /// <param name="id">Region ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest("Region ID is required");

            try
            {
                var region = await _officeRepository.GetRegionByIdAsync(id, CurrentOrganizationId);
                if (region == null)
                    return NotFound("Region not found");

                await _officeRepository.DeleteRegionByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting region: {RegionId}", id);
                return ServerError("An error occurred while deleting the region");
            }
        }
    }
}






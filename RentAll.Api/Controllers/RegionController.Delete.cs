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
                return BadRequest(new { message = "Region ID is required" });

            try
            {
                var region = await _regionRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (region == null)
                    return NotFound(new { message = "Region not found" });

                await _regionRepository.DeleteByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting region: {RegionId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the region" });
            }
        }
    }
}



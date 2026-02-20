using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class BuildingController
    {
        /// <summary>
        /// Delete a building
        /// </summary>
        /// <param name="id">Building ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest("Building ID is required");

            try
            {
                var building = await _officeRepository.GetBuildingByIdAsync(id, CurrentOrganizationId);
                if (building == null)
                    return NotFound("Building not found");

                await _officeRepository.DeleteBuildingByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting building: {BuildingId}", id);
                return ServerError("An error occurred while deleting the building");
            }
        }
    }
}






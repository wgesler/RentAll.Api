using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class AreaController
    {
        /// <summary>
        /// Delete an area
        /// </summary>
        /// <param name="id">Area ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Area ID is required" });

            try
            {
                var area = await _areaRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (area == null)
                    return NotFound(new { message = "Area not found" });

                await _areaRepository.DeleteByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting area: {AreaId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the area" });
            }
        }
    }
}





using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class FranchiseController
    {
        /// <summary>
        /// Delete a franchise
        /// </summary>
        /// <param name="id">Franchise ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Franchise ID is required" });

            try
            {
                var franchise = await _franchiseRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (franchise == null)
                    return NotFound(new { message = "Franchise not found" });

                await _franchiseRepository.DeleteByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting franchise: {FranchiseId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the franchise" });
            }
        }
    }
}



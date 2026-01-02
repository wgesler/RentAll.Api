using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Models;

namespace RentAll.Api.Controllers
{
    public partial class OfficeController
    {
		/// <summary>
		/// Delete an office
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <returns>No content</returns>
		[HttpDelete("{officeId}")]
        public async Task<IActionResult> Delete(int officeId)
        {
            if (officeId <= 0)
                return BadRequest(new { message = "Office ID is required" });

            try
            {
				await _officeConfigurationRepository.DeleteByOfficeIdAsync(officeId);
				await _officeRepository.DeleteByIdAsync(officeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting office: {OfficeId}", officeId);
                return StatusCode(500, new { message = "An error occurred while deleting the office" });
            }
        }
	}
}


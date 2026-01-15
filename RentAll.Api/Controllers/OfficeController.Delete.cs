using Microsoft.AspNetCore.Mvc;

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
                return BadRequest("Office ID is required");

            try
            {
				await _officeRepository.DeleteByIdAsync(officeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting office: {OfficeId}", officeId);
                return ServerError("An error occurred while deleting the office");
            }
        }
	}
}



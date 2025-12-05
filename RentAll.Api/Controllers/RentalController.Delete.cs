using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public partial class RentalController
	{
		/// <summary>
		/// Delete a rental
		/// </summary>
		/// <param name="id">Rental ID</param>
		/// <returns>No content</returns>
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(Guid id)
		{
			if (id == Guid.Empty)
				return BadRequest(new { message = "Rental ID is required" });

			try
			{
				// Check if rental exists
				var rental = await _rentalRepository.GetByIdAsync(id);
				if (rental == null)
					return NotFound(new { message = "Rental not found" });

				await _rentalRepository.DeleteByIdAsync(id);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting rental: {RentalId}", id);
				return StatusCode(500, new { message = "An error occurred while deleting the rental" });
			}
		}
	}
}


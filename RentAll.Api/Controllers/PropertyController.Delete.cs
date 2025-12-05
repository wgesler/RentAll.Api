using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public partial class PropertyController
	{
		/// <summary>
		/// Delete a property
		/// </summary>
		/// <param name="id">Property ID</param>
		/// <returns>No content</returns>
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(Guid id)
		{
			if (id == Guid.Empty)
				return BadRequest(new { message = "Property ID is required" });

			try
			{
				// Check if property exists
				var property = await _propertyRepository.GetByIdAsync(id);
				if (property == null)
					return NotFound(new { message = "Property not found" });

				await _propertyRepository.DeleteByIdAsync(id);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting property: {PropertyId}", id);
				return StatusCode(500, new { message = "An error occurred while deleting the property" });
			}
		}
	}
}


using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public partial class PropertyHtmlController
	{
		/// <summary>
		/// Delete a property HTML
		/// </summary>
		/// <param name="propertyId">Property ID</param>
		/// <returns>No content</returns>
		[HttpDelete("property/{propertyId}")]
		public async Task<IActionResult> Delete(Guid propertyId)
		{
			if (propertyId == Guid.Empty)
				return BadRequest(new { message = "Property ID is required" });

			try
			{
				// Verify property belongs to organization
				var property = await _propertyRepository.GetByIdAsync(propertyId, CurrentOrganizationId);
				if (property == null)
					return NotFound(new { message = "Property not found" });

				// Check if HTML exists
				var propertyHtml = await _propertyHtmlRepository.GetByPropertyIdAsync(propertyId, CurrentOrganizationId);
				if (propertyHtml == null)
					return NotFound(new { message = "Property HTML not found" });

				await _propertyHtmlRepository.DeleteByPropertyIdAsync(propertyId);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting property HTML: {PropertyId}", propertyId);
				return StatusCode(500, new { message = "An error occurred while deleting the property HTML" });
			}
		}
	}
}


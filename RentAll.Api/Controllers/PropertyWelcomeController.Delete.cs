using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public partial class PropertyWelcomeController
	{
		/// <summary>
		/// Delete a property welcome letter
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

				// Check if welcome exists
				var propertyWelcome = await _propertyWelcomeRepository.GetByPropertyIdAsync(propertyId, CurrentOrganizationId);
				if (propertyWelcome == null)
					return NotFound(new { message = "Property welcome not found" });

				await _propertyWelcomeRepository.DeleteByPropertyIdAsync(propertyId);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting property welcome: {PropertyId}", propertyId);
				return StatusCode(500, new { message = "An error occurred while deleting the property welcome" });
			}
		}
	}
}



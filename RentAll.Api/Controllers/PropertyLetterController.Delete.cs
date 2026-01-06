using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public partial class PropertyLetterController
	{
		/// <summary>
		/// Delete a property letter
		/// </summary>
		/// <param name="propertyId">Property ID</param>
		/// <returns>No content</returns>
		[HttpDelete("property/{propertyId}")]
		public async Task<IActionResult> Delete(Guid propertyId)
		{
			if (propertyId == Guid.Empty)
				return BadRequest("Property ID is required");

			try
			{
				// Verify property belongs to organization
				var property = await _propertyRepository.GetByIdAsync(propertyId, CurrentOrganizationId);
				if (property == null)
					return NotFound("Property not found");

				// Check if property letter exists
				var propertyLetter = await _propertyLetterRepository.GetByPropertyIdAsync(propertyId, CurrentOrganizationId);
				if (propertyLetter == null)
					return NotFound("Property letter not found");

				await _propertyLetterRepository.DeleteByPropertyIdAsync(propertyId);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting property letter: {PropertyId}", propertyId);
				return ServerError("An error occurred while deleting the property letter");
			}
		}
	}
}



using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.PropertyLetters;

namespace RentAll.Api.Controllers
{
	public partial class PropertyLetterController
	{
		/// <summary>
		/// Get property letter by Property ID
		/// </summary>
		/// <param name="propertyId">Property ID</param>
		/// <returns>Property letter</returns>
		[HttpGet("{propertyId}")]
		public async Task<IActionResult> GetByPropertyId(Guid propertyId)
		{
			if (propertyId == Guid.Empty)
				return BadRequest("Property ID is required");

			try
			{
				// Verify property belongs to organization
				var property = await _propertyRepository.GetByIdAsync(propertyId, CurrentOrganizationId);
				if (property == null)
					return NotFound("Property not found");

				var propertyLetter = await _propertyLetterRepository.GetByPropertyIdAsync(propertyId, CurrentOrganizationId);
				if (propertyLetter == null)
					return NotFound("Property letter not found");

				return Ok(new PropertyLetterResponseDto(propertyLetter));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting property letter by Property ID: {PropertyId}", propertyId);
				return ServerError("An error occurred while retrieving the property letter");
			}
		}
	}
}



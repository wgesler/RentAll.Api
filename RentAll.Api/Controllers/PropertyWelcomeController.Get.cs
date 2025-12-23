using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.PropertyWelcomes;

namespace RentAll.Api.Controllers
{
	public partial class PropertyWelcomeController
	{
		/// <summary>
		/// Get property welcome letter by Property ID
		/// </summary>
		/// <param name="propertyId">Property ID</param>
		/// <returns>Property welcome letter</returns>
		[HttpGet("{propertyId}")]
		public async Task<IActionResult> GetByPropertyId(Guid propertyId)
		{
			if (propertyId == Guid.Empty)
				return BadRequest(new { message = "Property ID is required" });

			try
			{
				// Verify property belongs to organization
				var property = await _propertyRepository.GetByIdAsync(propertyId, CurrentOrganizationId);
				if (property == null)
					return NotFound(new { message = "Property not found" });

				var propertyWelcome = await _propertyWelcomeRepository.GetByPropertyIdAsync(propertyId, CurrentOrganizationId);
				if (propertyWelcome == null)
					return NotFound(new { message = "Property welcome not found" });

				return Ok(new PropertyWelcomeResponseDto(propertyWelcome));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting property welcome by Property ID: {PropertyId}", propertyId);
				return StatusCode(500, new { message = "An error occurred while retrieving the property welcome" });
			}
		}
	}
}



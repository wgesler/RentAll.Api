using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.PropertyHtmls;

namespace RentAll.Api.Controllers
{
	public partial class PropertyHtmlController
	{
		/// <summary>
		/// Get property HTML by Property ID
		/// </summary>
		/// <param name="propertyId">Property ID</param>
		/// <returns>Property HTML</returns>
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

				var propertyHtml = await _propertyHtmlRepository.GetByPropertyIdAsync(propertyId, CurrentOrganizationId);
				if (propertyHtml == null)
					return NotFound(new { message = "Property HTML not found" });

				return Ok(new PropertyHtmlResponseDto(propertyHtml));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting property HTML by Property ID: {PropertyId}", propertyId);
				return StatusCode(500, new { message = "An error occurred while retrieving the property HTML" });
			}
		}
	}
}


using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.PropertyHtmls;

namespace RentAll.Api.Controllers
{
	public partial class PropertyHtmlController
	{
		/// <summary>
		/// Create a new property HTML
		/// </summary>
		/// <param name="dto">Property HTML data</param>
		/// <returns>Created property HTML</returns>
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreatePropertyHtmlDto dto)
		{
			if (dto == null)
				return BadRequest(new { message = "Property HTML data is required" });

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(new { message = errorMessage });

			try
			{
				// Verify property belongs to organization
				var property = await _propertyRepository.GetByIdAsync(dto.PropertyId, CurrentOrganizationId);
				if (property == null)
					return NotFound(new { message = "Property not found" });

				var propertyHtml = dto.ToModel(CurrentUser);
				var createdPropertyHtml = await _propertyHtmlRepository.CreateAsync(propertyHtml);
				return CreatedAtAction(nameof(GetByPropertyId), new { propertyId = createdPropertyHtml.PropertyId }, new PropertyHtmlResponseDto(createdPropertyHtml));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating property HTML");
				return StatusCode(500, new { message = "An error occurred while creating the property HTML" });
			}
		}
	}
}


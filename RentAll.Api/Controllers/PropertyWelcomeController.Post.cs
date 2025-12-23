using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.PropertyWelcomes;

namespace RentAll.Api.Controllers
{
	public partial class PropertyWelcomeController
	{
		/// <summary>
		/// Create a new property welcome letter
		/// </summary>
		/// <param name="dto">Property welcome data</param>
		/// <returns>Created property welcome</returns>
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreatePropertyWelcomeDto dto)
		{
			if (dto == null)
				return BadRequest(new { message = "Property welcome data is required" });

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(new { message = errorMessage });

			try
			{
				// Verify property belongs to organization
				var property = await _propertyRepository.GetByIdAsync(dto.PropertyId, CurrentOrganizationId);
				if (property == null)
					return NotFound(new { message = "Property not found" });

				var propertyWelcome = dto.ToModel(CurrentUser);
				var createdPropertyWelcome = await _propertyWelcomeRepository.CreateAsync(propertyWelcome);
				return CreatedAtAction(nameof(GetByPropertyId), new { propertyId = createdPropertyWelcome.PropertyId }, new PropertyWelcomeResponseDto(createdPropertyWelcome));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating property welcome");
				return StatusCode(500, new { message = "An error occurred while creating the property welcome" });
			}
		}
	}
}



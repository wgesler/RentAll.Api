using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.PropertyWelcomes;

namespace RentAll.Api.Controllers
{
	public partial class PropertyWelcomeController
	{
		/// <summary>
		/// Update an existing property welcome letter
		/// </summary>
		/// <param name="dto">Property welcome data</param>
		/// <returns>Updated property welcome</returns>
		[HttpPut]
		public async Task<IActionResult> Update([FromBody] UpdatePropertyWelcomeDto dto)
		{
			if (dto == null)
				return BadRequest(new { message = "Property welcome data is required" });

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(new { message = errorMessage });

			try
			{
				// Check if welcome exists
				var existing = await _propertyWelcomeRepository.GetByPropertyIdAsync(dto.PropertyId, CurrentOrganizationId);
				if (existing == null)
					return NotFound(new { message = "Property welcome not found" });

				var propertyWelcome = dto.ToModel(CurrentUser, CurrentOrganizationId);
				var updatedPropertyWelcome = await _propertyWelcomeRepository.UpdateByIdAsync(propertyWelcome);
				return Ok(new PropertyWelcomeResponseDto(updatedPropertyWelcome));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating property welcome: {PropertyId}", dto.PropertyId);
				return StatusCode(500, new { message = "An error occurred while updating the property welcome" });
			}
		}
	}
}



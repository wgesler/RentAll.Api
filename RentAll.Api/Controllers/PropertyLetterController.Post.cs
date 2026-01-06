using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.PropertyLetters;

namespace RentAll.Api.Controllers
{
	public partial class PropertyLetterController
	{
		/// <summary>
		/// Create a new property letter
		/// </summary>
		/// <param name="dto">Property letter data</param>
		/// <returns>Created property letter</returns>
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreatePropertyLetterDto dto)
		{
			if (dto == null)
				return BadRequest("Property letter data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				// Verify property belongs to organization
				var property = await _propertyRepository.GetByIdAsync(dto.PropertyId, CurrentOrganizationId);
				if (property == null)
					return NotFound("Property not found");

				var propertyLetter = dto.ToModel(CurrentUser);
				var createdPropertyLetter = await _propertyLetterRepository.CreateAsync(propertyLetter);
				return CreatedAtAction(nameof(GetByPropertyId), new { propertyId = createdPropertyLetter.PropertyId }, new PropertyLetterResponseDto(createdPropertyLetter));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating property letter");
				return ServerError("An error occurred while creating the property letter");
			}
		}
	}
}



using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.PropertyLetters;

namespace RentAll.Api.Controllers
{
	public partial class PropertyLetterController
	{
		/// <summary>
		/// Update an existing property letter
		/// </summary>
		/// <param name="dto">Property letter data</param>
		/// <returns>Updated property letter</returns>
		[HttpPut]
		public async Task<IActionResult> Update([FromBody] UpdatePropertyLetterDto dto)
		{
			if (dto == null)
				return BadRequest(new { message = "Property letter data is required" });

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(new { message = errorMessage });

			try
			{
				// Check if property letter exists
				var existing = await _propertyLetterRepository.GetByPropertyIdAsync(dto.PropertyId, CurrentOrganizationId);
				if (existing == null)
					return NotFound(new { message = "Property letter not found" });

				var propertyLetter = dto.ToModel(CurrentUser);
				var updatedPropertyLetter = await _propertyLetterRepository.UpdateByIdAsync(propertyLetter);
				return Ok(new PropertyLetterResponseDto(updatedPropertyLetter));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating property letter: {PropertyId}", dto.PropertyId);
				return StatusCode(500, new { message = "An error occurred while updating the property letter" });
			}
		}
	}
}


using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Colors;

namespace RentAll.Api.Controllers
{
	public partial class ColorController
	{
		/// <summary>
		/// Update color for organization and reservation status
		/// </summary>
		/// <param name="dto">Color data</param>
		/// <returns>Updated color</returns>
		[HttpPut]
		public async Task<IActionResult> Update([FromBody] UpdateColorDto dto)
		{
			if (dto == null)
				return BadRequest("Color data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid request data");

			var existingColor = dto.ToModel();

			await _colorRepository.UpdateByIdAsync(existingColor);
			return Ok();
		}
	}
}


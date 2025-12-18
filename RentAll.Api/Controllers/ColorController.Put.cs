using System.Text.RegularExpressions;
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
				return BadRequest(new { message = "Color data is required" });

			if (dto.ReservationStatusId < 0)
				return BadRequest(new { message = "ReservationStatusId is required" });

			if (string.IsNullOrWhiteSpace(dto.Color))
				return BadRequest(new { message = "Color value is required" });

			// Remove # prefix if present
			var colorValue = dto.Color.TrimStart('#');

			// Validate Color format (should be 6 hex characters)
			if (colorValue.Length != 6 || !Regex.IsMatch(colorValue, @"^[0-9A-Fa-f]{6}$"))
				return BadRequest(new { message = "Color must be a 6-character hexadecimal value (e.g., FF0000 or #FF0000)" });

			var existingColor = dto.ToModel();
	
			await _colorRepository.UpdateByIdAsync(existingColor);
			return Ok();
		}
	}
}


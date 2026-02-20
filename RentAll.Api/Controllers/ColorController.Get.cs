using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Colors;

namespace RentAll.Api.Controllers
{
    public partial class ColorController
    {
        /// <summary>
        /// Get all colors for the current organization
        /// </summary>
        /// <returns>List of colors</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var colors = await _officeRepository.GetAllColorsAsync(CurrentOrganizationId);
                var response = colors.Select(c => new ColorResponseDto(c));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all colors");
                return ServerError("An error occurred while retrieving colors");
            }
        }

        /// <summary>
        /// Get color by ColorId
        /// </summary>
        /// <param name="ColorId">Color Id</param>
        /// <returns>Color</returns>
        [HttpGet("{colorId}")]
        public async Task<IActionResult> GetById(int colorId)
        {
            if (colorId < 0)
                return BadRequest("Invalid ColorId");

            var color = await _officeRepository.GetColorByIdAsync(colorId, CurrentOrganizationId);
            if (color == null)
                return NotFound("Color not found");

            var response = new ColorResponseDto(color);
            return Ok(response);
        }
    }
}


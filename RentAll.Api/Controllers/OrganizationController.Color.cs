
namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        #region Get

        [HttpGet("color")]
        public async Task<IActionResult> GetAllColors()
        {
            try
            {
                var colors = await _organizationRepository.GetColorsByOrganizationIdAsync(CurrentOrganizationId);
                var response = colors.Select(c => new ColorResponseDto(c));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all colors");
                return ServerError("An error occurred while retrieving colors");
            }
        }

        [HttpGet("color/{colorId}")]
        public async Task<IActionResult> GetColorById(int colorId)
        {
            if (colorId < 0)
                return BadRequest("Invalid ColorId");

            var color = await _organizationRepository.GetColorByIdAsync(colorId, CurrentOrganizationId);
            if (color == null)
                return NotFound("Color not found");

            var response = new ColorResponseDto(color);
            return Ok(response);
        }

        #endregion

        #region Put

        [HttpPut("color")]
        public async Task<IActionResult> UpdateColor([FromBody] UpdateColorDto dto)
        {
            if (dto == null)
                return BadRequest("Color data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            var existingColor = dto.ToModel();

            await _organizationRepository.UpdateColorByIdAsync(existingColor);
            return Ok();
        }

        #endregion

    }
}

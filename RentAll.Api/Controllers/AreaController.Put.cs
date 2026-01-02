using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Areas;

namespace RentAll.Api.Controllers
{
    public partial class AreaController
    {
        /// <summary>
        /// Update an existing area
        /// </summary>
        /// <param name="id">Area ID</param>
        /// <param name="dto">Area data</param>
        /// <returns>Updated area</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AreaUpdateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Area data is required" });

            if (id != dto.AreaId)
                return BadRequest(new { message = "Area ID mismatch" });

            if (string.IsNullOrWhiteSpace(dto.AreaCode))
                return BadRequest(new { message = "Area Code is required" });

            try
            {
                var existingArea = await _areaRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (existingArea == null)
                    return NotFound(new { message = "Area not found" });

				// Check if AgentCode is being changed and if the new code already exists
				if (existingArea.AreaCode != dto.AreaCode)
				{
					if (await _areaRepository.ExistsByAreaCodeAsync(dto.AreaCode, CurrentOrganizationId, dto.OfficeId))
						return Conflict(new { message = "Area Code already exists" });
				}
				
				var area = dto.ToModel();
                var updatedArea = await _areaRepository.UpdateByIdAsync(area);
                return Ok(new AreaResponseDto(updatedArea));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating area: {AreaId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the area" });
            }
        }
    }
}





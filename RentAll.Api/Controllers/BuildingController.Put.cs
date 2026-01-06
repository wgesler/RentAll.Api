using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Buildings;

namespace RentAll.Api.Controllers
{
    public partial class BuildingController
    {
        /// <summary>
        /// Update an existing building
        /// </summary>
        /// <param name="id">Building ID</param>
        /// <param name="dto">Building data</param>
        /// <returns>Updated building</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BuildingUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Building data is required");

            if (id != dto.BuildingId)
                return BadRequest("Building ID mismatch");

            if (string.IsNullOrWhiteSpace(dto.BuildingCode))
                return BadRequest("Building Code is required");

            try
            {
                var existingBuilding = await _buildingRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (existingBuilding == null)
                    return NotFound("Building not found");

				// Check if BuildingCode is being changed and if the new code already exists
				if (existingBuilding.BuildingCode != dto.BuildingCode)
				{
					if (await _buildingRepository.ExistsByBuildingCodeAsync(dto.BuildingCode, CurrentOrganizationId, dto.OfficeId))
						return Conflict("Building Code already exists");
				}

				var building = dto.ToModel();
                var updatedBuilding = await _buildingRepository.UpdateByIdAsync(building);
                return Ok(new BuildingResponseDto(updatedBuilding));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating building: {BuildingId}", id);
                return ServerError("An error occurred while updating the building");
            }
        }
    }
}






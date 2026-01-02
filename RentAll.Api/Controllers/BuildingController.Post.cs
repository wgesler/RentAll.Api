using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Buildings;
using RentAll.Api.Dtos.Properties;

namespace RentAll.Api.Controllers
{
    public partial class BuildingController
    {
        /// <summary>
        /// Create a new building
        /// </summary>
        /// <param name="dto">Building data</param>
        /// <returns>Created building</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BuildingCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Building data is required" });

            try
            {
                if (await _buildingRepository.ExistsByBuildingCodeAsync(dto.BuildingCode, CurrentOrganizationId, dto.OfficeId))
                    return Conflict(new { message = "Building Code already exists" });

                var building = dto.ToModel();
                var createdBuilding = await _buildingRepository.CreateAsync(building);
                return CreatedAtAction(nameof(GetById), new { id = createdBuilding.BuildingId }, new BuildingResponseDto(createdBuilding));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating building");
                return StatusCode(500, new { message = "An error occurred while creating the building" });
            }
        }
    }
}





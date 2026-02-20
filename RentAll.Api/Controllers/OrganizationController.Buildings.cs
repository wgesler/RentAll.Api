
namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {

        #region Get

        /// <summary>
        /// Get all buildings
        /// </summary>
        /// <returns>List of buildings</returns>
        [HttpGet("buildings")]
        public async Task<IActionResult> GetAllBuildings()
        {
            try
            {
                var buildings = await _organizationRepository.GetAllBuildingsByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = buildings.Select(b => new BuildingResponseDto(b));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all buildings");
                return ServerError("An error occurred while retrieving buildings");
            }
        }

        /// <summary>
        /// Get building by ID
        /// </summary>
        /// <param name="BuildingId">Building ID</param>
        /// <returns>Building</returns>
        [HttpGet("buildings/{BuildingId}")]
        public async Task<IActionResult> GetBuildingById(int BuildingId)
        {
            if (BuildingId <= 0)
                return BadRequest("Building ID is required");

            try
            {
                var building = await _organizationRepository.GetBuildingByIdAsync(BuildingId, CurrentOrganizationId);
                if (building == null)
                    return NotFound("Building not found");

                return Ok(new BuildingResponseDto(building));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting building by ID: {BuildingId}", BuildingId);
                return ServerError("An error occurred while retrieving the building");
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Create a new building
        /// </summary>
        /// <param name="dto">Building data</param>
        /// <returns>Created building</returns>
        [HttpPost("buildings")]
        public async Task<IActionResult> CreateBuilding([FromBody] BuildingCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Building data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                if (await _organizationRepository.ExistsBuildingByCodeAsync(dto.BuildingCode, CurrentOrganizationId, dto.OfficeId))
                    return Conflict("Building Code already exists");

                var building = dto.ToModel();
                var createdBuilding = await _organizationRepository.CreateBuildingAsync(building);
                return CreatedAtAction(nameof(GetBuildingById), new { id = createdBuilding.BuildingId }, new BuildingResponseDto(createdBuilding));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating building");
                return ServerError("An error occurred while creating the building");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing building
        /// </summary>
        /// <param name="dto">Building data</param>
        /// <returns>Updated building</returns>
        [HttpPut("buildings")]
        public async Task<IActionResult> UpdateBuilding([FromBody] BuildingUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Building data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingBuilding = await _organizationRepository.GetBuildingByIdAsync(dto.BuildingId, CurrentOrganizationId);
                if (existingBuilding == null)
                    return NotFound("Building not found");

                // Check if BuildingCode is being changed and if the new code already exists
                if (existingBuilding.BuildingCode != dto.BuildingCode)
                {
                    if (await _organizationRepository.ExistsBuildingByCodeAsync(dto.BuildingCode, CurrentOrganizationId, dto.OfficeId))
                        return Conflict("Building Code already exists");
                }

                var building = dto.ToModel();
                var updatedBuilding = await _organizationRepository.UpdateBuildingByIdAsync(building);
                return Ok(new BuildingResponseDto(updatedBuilding));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating building: {BuildingId}", dto.BuildingId);
                return ServerError("An error occurred while updating the building");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete a building
        /// </summary>
        /// <param name="buildingId">Building ID</param>
        /// <returns>No content</returns>
        [HttpDelete("buildings/{buildingId}")]
        public async Task<IActionResult> DeleteBuilding(int buildingId)
        {
            if (buildingId <= 0)
                return BadRequest("Building ID is required");

            try
            {
                var building = await _organizationRepository.GetBuildingByIdAsync(buildingId, CurrentOrganizationId);
                if (building == null)
                    return NotFound("Building not found");

                await _organizationRepository.DeleteBuildingByIdAsync(buildingId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting building: {BuildingId}", buildingId);
                return ServerError("An error occurred while deleting the building");
            }
        }

        #endregion

    }
}


namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {

        #region Get

        /// <summary>
        /// Get all areas
        /// </summary>
        /// <returns>List of areas</returns>
        [HttpGet("areas")]
        public async Task<IActionResult> GetAllAreas()
        {
            try
            {
                var areas = await _organizationRepository.GetAllAreasByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = areas.Select(a => new AreaResponseDto(a));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all areas");
                return ServerError("An error occurred while retrieving areas");
            }
        }

        /// <summary>
        /// Get area by ID
        /// </summary>
        /// <param name="areaId">Area ID</param>
        /// <returns>Area</returns>
        [HttpGet("areas/{areaId}")]
        public async Task<IActionResult> GetAreaById(int areaId)
        {
            if (areaId <= 0)
                return BadRequest("Area ID is required");

            try
            {
                var area = await _organizationRepository.GetAreaByIdAsync(areaId, CurrentOrganizationId);
                if (area == null)
                    return NotFound("Area not found");

                return Ok(new AreaResponseDto(area));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting area by ID: {AreaId}", areaId);
                return ServerError("An error occurred while retrieving the area");
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Create a new area
        /// </summary>
        /// <param name="dto">Area data</param>
        /// <returns>Created area</returns>
        [HttpPost("areas")]
        public async Task<IActionResult> CreateArea([FromBody] AreaCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Area data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                if (await _organizationRepository.ExistsAreaByCodeAsync(dto.AreaCode, CurrentOrganizationId, dto.OfficeId))
                    return Conflict("Area Code already exists");

                var area = dto.ToModel();
                var createdArea = await _organizationRepository.CreateAreaAsync(area);
                return CreatedAtAction(nameof(GetAreaById), new { id = createdArea.AreaId }, new AreaResponseDto(createdArea));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating area");
                return ServerError("An error occurred while creating the area");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing area
        /// </summary>
        /// <param name="dto">Area data</param>
        /// <returns>Updated area</returns>
        [HttpPut("areas")]
        public async Task<IActionResult> UpdateArea([FromBody] AreaUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Area data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingArea = await _organizationRepository.GetAreaByIdAsync(dto.AreaId, CurrentOrganizationId);
                if (existingArea == null)
                    return NotFound("Area not found");

                // Check if AgentCode is being changed and if the new code already exists
                if (existingArea.AreaCode != dto.AreaCode)
                {
                    if (await _organizationRepository.ExistsAreaByCodeAsync(dto.AreaCode, CurrentOrganizationId, dto.OfficeId))
                        return Conflict("Area Code already exists");
                }

                var area = dto.ToModel();
                var updatedArea = await _organizationRepository.UpdateAreaByIdAsync(area);
                return Ok(new AreaResponseDto(updatedArea));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating area: {AreaId}", dto.AreaId);
                return ServerError("An error occurred while updating the area");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete an area
        /// </summary>
        /// <param name="areaId">Area ID</param>
        /// <returns>No content</returns>
        [HttpDelete("areas/{areaId}")]
        public async Task<IActionResult> DeleteArea(int areaId)
        {
            if (areaId <= 0)
                return BadRequest("Area ID is required");

            try
            {
                var area = await _organizationRepository.GetAreaByIdAsync(areaId, CurrentOrganizationId);
                if (area == null)
                    return NotFound("Area not found");

                await _organizationRepository.DeleteAreaByIdAsync(areaId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting area: {AreaId}", areaId);
                return ServerError("An error occurred while deleting the area");
            }
        }

        #endregion

    }
}

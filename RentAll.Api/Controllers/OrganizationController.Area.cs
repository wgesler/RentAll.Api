
namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {

        #region Get

        [HttpGet("area")]
        public async Task<IActionResult> GetAllAreas()
        {
            try
            {
                var areas = await _organizationRepository.GetAreasByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = areas.Select(a => new AreaResponseDto(a));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all areas");
                return ServerError("An error occurred while retrieving areas");
            }
        }

        [HttpGet("area/{areaId}")]
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

        [HttpPost("area")]
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

                var response = new AreaResponseDto(createdArea);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating area");
                return ServerError("An error occurred while creating the area");
            }
        }

        #endregion

        #region Put

        [HttpPut("area")]
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

        [HttpDelete("area/{areaId}")]
        public async Task<IActionResult> DeleteAreaByIdAsync(int areaId)
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

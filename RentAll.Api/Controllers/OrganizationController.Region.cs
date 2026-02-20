
namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {

        #region Get

        /// <summary>
        /// Get all regions
        /// </summary>
        /// <returns>List of regions</returns>
        [HttpGet("region")]
        public async Task<IActionResult> GetAllRegions()
        {
            try
            {
                var regions = await _organizationRepository.GetAllRegionsByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = regions.Select(r => new RegionResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all regions");
                return ServerError("An error occurred while retrieving regions");
            }
        }

        /// <summary>
        /// Get region by ID
        /// </summary>
        /// <param name="regionId">Region ID</param>
        /// <returns>Region</returns>
        [HttpGet("region/{regionId}")]
        public async Task<IActionResult> GetRegionById(int regionId)
        {
            System.Diagnostics.Debugger.Break();
            if (regionId <= 0)
                return BadRequest("Region ID is required");

            try
            {
                var region = await _organizationRepository.GetRegionByIdAsync(regionId, CurrentOrganizationId);
                if (region == null)
                    return NotFound("Region not found");

                return Ok(new RegionResponseDto(region));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting region by ID: {RegionId}", regionId);
                return ServerError("An error occurred while retrieving the region");
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Create a new region
        /// </summary>
        /// <param name="dto">Region data</param>
        /// <returns>Created region</returns>
        [HttpPost("region")]
        public async Task<IActionResult> CreateRegion([FromBody] RegionCreateDto dto)
        {
            System.Diagnostics.Debugger.Break();
            if (dto == null)
                return BadRequest("Region data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                if (await _organizationRepository.ExistsRegionByCodeAsync(dto.RegionCode, CurrentOrganizationId, dto.OfficeId))
                    return Conflict("Region Code already exists");

                var region = dto.ToModel();
                var createdRegion = await _organizationRepository.CreateRegionAsync(region);
                return CreatedAtAction(nameof(GetRegionById), new { id = createdRegion.RegionId }, new RegionResponseDto(createdRegion));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating region");
                return ServerError("An error occurred while creating the region");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing region
        /// </summary>
        /// <param name="dto">Region data</param>
        /// <returns>Updated region</returns>
        [HttpPut("region")]
        public async Task<IActionResult> UpdateRegion([FromBody] RegionUpdateDto dto)
        {
            System.Diagnostics.Debugger.Break();
            if (dto == null)
                return BadRequest("Region data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingRegion = await _organizationRepository.GetRegionByIdAsync(dto.RegionId, CurrentOrganizationId);
                if (existingRegion == null)
                    return NotFound("Region not found");

                if (existingRegion.RegionCode != dto.RegionCode)
                {
                    if (await _organizationRepository.ExistsRegionByCodeAsync(dto.RegionCode, CurrentOrganizationId, dto.OfficeId))
                        return Conflict("Region Code already exists");
                }

                var region = dto.ToModel();
                var updatedRegion = await _organizationRepository.UpdateRegionByIdAsync(region);
                return Ok(new RegionResponseDto(updatedRegion));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating region: {RegionId}", dto.RegionId);
                return ServerError("An error occurred while updating the region");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete a region
        /// </summary>
        /// <param name="regionId">Region ID</param>
        /// <returns>No content</returns>
        [HttpDelete("region/{regionId}")]
        public async Task<IActionResult> DeleteRegion(int regionId)
        {
            System.Diagnostics.Debugger.Break();
            if (regionId <= 0)
                return BadRequest("Region ID is required");

            try
            {
                var region = await _organizationRepository.GetRegionByIdAsync(regionId, CurrentOrganizationId);
                if (region == null)
                    return NotFound("Region not found");

                await _organizationRepository.DeleteRegionByIdAsync(regionId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting region: {RegionId}", regionId);
                return ServerError("An error occurred while deleting the region");
            }
        }

        #endregion

    }
}

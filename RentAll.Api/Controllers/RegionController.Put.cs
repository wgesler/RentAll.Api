using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Properties;
using RentAll.Api.Dtos.Regions;

namespace RentAll.Api.Controllers
{
    public partial class RegionController
    {
        /// <summary>
        /// Update an existing region
        /// </summary>
        /// <param name="id">Region ID</param>
        /// <param name="dto">Region data</param>
        /// <returns>Updated region</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RegionUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Region data is required");

            if (id != dto.RegionId)
                return BadRequest("Region ID mismatch");

            if (string.IsNullOrWhiteSpace(dto.RegionCode))
                return BadRequest("Region Code is required");

            try
            {
                var existingRegion = await _regionRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (existingRegion == null)
                    return NotFound("Region not found");

                if (existingRegion.RegionCode != dto.RegionCode)
                {
                    if (await _regionRepository.ExistsByRegionCodeAsync(dto.RegionCode, CurrentOrganizationId, dto.OfficeId))
                        return Conflict("Region Code already exists");
                }

                var region = dto.ToModel();
                var updatedRegion = await _regionRepository.UpdateByIdAsync(region);
                return Ok(new RegionResponseDto(updatedRegion));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating region: {RegionId}", id);
                return ServerError("An error occurred while updating the region");
            }
        }
    }
}






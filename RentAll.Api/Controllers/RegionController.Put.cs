using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Organizations.Regions;

namespace RentAll.Api.Controllers
{
    public partial class RegionController
    {
        /// <summary>
        /// Update an existing region
        /// </summary>
        /// <param name="dto">Region data</param>
        /// <returns>Updated region</returns>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] RegionUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Region data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingRegion = await _officeRepository.GetRegionByIdAsync(dto.RegionId, CurrentOrganizationId);
                if (existingRegion == null)
                    return NotFound("Region not found");

                if (existingRegion.RegionCode != dto.RegionCode)
                {
                    if (await _officeRepository.ExistsRegionByCodeAsync(dto.RegionCode, CurrentOrganizationId, dto.OfficeId))
                        return Conflict("Region Code already exists");
                }

                var region = dto.ToModel();
                var updatedRegion = await _officeRepository.UpdateRegionByIdAsync(region);
                return Ok(new RegionResponseDto(updatedRegion));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating region: {RegionId}", dto.RegionId);
                return ServerError("An error occurred while updating the region");
            }
        }
    }
}






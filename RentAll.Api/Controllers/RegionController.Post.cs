using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Properties;
using RentAll.Api.Dtos.Regions;

namespace RentAll.Api.Controllers
{
    public partial class RegionController
    {
        /// <summary>
        /// Create a new region
        /// </summary>
        /// <param name="dto">Region data</param>
        /// <returns>Created region</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RegionCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Region data is required" });

            if (string.IsNullOrWhiteSpace(dto.RegionCode))
                return BadRequest(new { message = "Region Code is required" });

            try
            {
                if (await _regionRepository.ExistsByRegionCodeAsync(dto.RegionCode, CurrentOrganizationId, dto.OfficeId))
                    return Conflict(new { message = "Region Code already exists" });

                var region = dto.ToModel();
                var createdRegion = await _regionRepository.CreateAsync(region);
                return CreatedAtAction(nameof(GetById), new { id = createdRegion.RegionId }, new RegionResponseDto(createdRegion));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating region");
                return StatusCode(500, new { message = "An error occurred while creating the region" });
            }
        }
    }
}





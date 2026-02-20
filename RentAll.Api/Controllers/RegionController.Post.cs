using Microsoft.AspNetCore.Mvc;
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
                return BadRequest("Region data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                if (await _officeRepository.ExistsRegionByCodeAsync(dto.RegionCode, CurrentOrganizationId, dto.OfficeId))
                    return Conflict("Region Code already exists");

                var region = dto.ToModel();
                var createdRegion = await _officeRepository.CreateRegionAsync(region);
                return CreatedAtAction(nameof(GetById), new { id = createdRegion.RegionId }, new RegionResponseDto(createdRegion));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating region");
                return ServerError("An error occurred while creating the region");
            }
        }
    }
}






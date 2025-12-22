using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Areas;

namespace RentAll.Api.Controllers
{
    public partial class AreaController
    {
        /// <summary>
        /// Create a new area
        /// </summary>
        /// <param name="dto">Area data</param>
        /// <returns>Created area</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AreaCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Area data is required" });

            if (string.IsNullOrWhiteSpace(dto.AreaCode))
                return BadRequest(new { message = "Area Code is required" });

            try
            {
                if (await _areaRepository.ExistsByAreaCodeAsync(dto.AreaCode, CurrentOrganizationId))
                    return Conflict(new { message = "Area Code already exists" });

                var area = dto.ToModel();
                var createdArea = await _areaRepository.CreateAsync(area);
                return CreatedAtAction(nameof(GetById), new { id = createdArea.AreaId }, new AreaResponseDto(createdArea));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating area");
                return StatusCode(500, new { message = "An error occurred while creating the area" });
            }
        }
    }
}





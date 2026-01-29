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
                return BadRequest("Area data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                if (await _areaRepository.ExistsByAreaCodeAsync(dto.AreaCode, CurrentOrganizationId, dto.OfficeId))
                    return Conflict("Area Code already exists");

                var area = dto.ToModel();
                var createdArea = await _areaRepository.CreateAsync(area);
                return CreatedAtAction(nameof(GetById), new { id = createdArea.AreaId }, new AreaResponseDto(createdArea));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating area");
                return ServerError("An error occurred while creating the area");
            }
        }
    }
}






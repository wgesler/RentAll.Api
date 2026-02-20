using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Areas;

namespace RentAll.Api.Controllers
{
    public partial class AreaController
    {
        /// <summary>
        /// Update an existing area
        /// </summary>
        /// <param name="dto">Area data</param>
        /// <returns>Updated area</returns>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] AreaUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Area data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingArea = await _officeRepository.GetAreaByIdAsync(dto.AreaId, CurrentOrganizationId);
                if (existingArea == null)
                    return NotFound("Area not found");

                // Check if AgentCode is being changed and if the new code already exists
                if (existingArea.AreaCode != dto.AreaCode)
                {
                    if (await _officeRepository.ExistsAreaByCodeAsync(dto.AreaCode, CurrentOrganizationId, dto.OfficeId))
                        return Conflict("Area Code already exists");
                }

                var area = dto.ToModel();
                var updatedArea = await _officeRepository.UpdateAreaByIdAsync(area);
                return Ok(new AreaResponseDto(updatedArea));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating area: {AreaId}", dto.AreaId);
                return ServerError("An error occurred while updating the area");
            }
        }
    }
}






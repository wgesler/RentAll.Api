using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Areas;

namespace RentAll.Api.Controllers
{
    public partial class AreaController
    {
        /// <summary>
        /// Get all areas
        /// </summary>
        /// <returns>List of areas</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var areas = await _areaRepository.GetAllAsync(CurrentOrganizationId);
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
        /// <param name="id">Area ID</param>
        /// <returns>Area</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest("Area ID is required");

            try
            {
                var area = await _areaRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (area == null)
                    return NotFound("Area not found");

                return Ok(new AreaResponseDto(area));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting area by ID: {AreaId}", id);
                return ServerError("An error occurred while retrieving the area");
            }
        }
    }
}






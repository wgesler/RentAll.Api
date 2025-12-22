using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Regions;

namespace RentAll.Api.Controllers
{
    public partial class RegionController
    {
        /// <summary>
        /// Get all regions
        /// </summary>
        /// <returns>List of regions</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var regions = await _regionRepository.GetAllAsync(CurrentOrganizationId);
                var response = regions.Select(r => new RegionResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all regions");
                return StatusCode(500, new { message = "An error occurred while retrieving regions" });
            }
        }

        /// <summary>
        /// Get region by ID
        /// </summary>
        /// <param name="id">Region ID</param>
        /// <returns>Region</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Region ID is required" });

            try
            {
                var region = await _regionRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (region == null)
                    return NotFound(new { message = "Region not found" });

                return Ok(new RegionResponseDto(region));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting region by ID: {RegionId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the region" });
            }
        }
    }
}





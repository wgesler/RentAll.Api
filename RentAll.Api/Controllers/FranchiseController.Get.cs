using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Franchises;

namespace RentAll.Api.Controllers
{
    public partial class FranchiseController
    {
        /// <summary>
        /// Get all franchises
        /// </summary>
        /// <returns>List of franchises</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var franchises = await _franchiseRepository.GetAllAsync(CurrentOrganizationId);
                var response = franchises.Select(f => new FranchiseResponseDto(f));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all franchises");
                return StatusCode(500, new { message = "An error occurred while retrieving franchises" });
            }
        }

        /// <summary>
        /// Get franchise by ID
        /// </summary>
        /// <param name="id">Franchise ID</param>
        /// <returns>Franchise</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Franchise ID is required" });

            try
            {
                var franchise = await _franchiseRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (franchise == null)
                    return NotFound(new { message = "Franchise not found" });

                return Ok(new FranchiseResponseDto(franchise));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting franchise by ID: {FranchiseId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the franchise" });
            }
        }
    }
}



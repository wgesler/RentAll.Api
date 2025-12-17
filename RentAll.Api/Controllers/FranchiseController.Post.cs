using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Franchises;
using RentAll.Api.Dtos.Properties;

namespace RentAll.Api.Controllers
{
    public partial class FranchiseController
    {
        /// <summary>
        /// Create a new franchise
        /// </summary>
        /// <param name="dto">Franchise data</param>
        /// <returns>Created franchise</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FranchiseCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Franchise data is required" });

            if (string.IsNullOrWhiteSpace(dto.FranchiseCode))
                return BadRequest(new { message = "Franchise Code is required" });

            try
            {
                if (await _franchiseRepository.ExistsByFranchiseCodeAsync(dto.FranchiseCode, CurrentOrganizationId))
                    return Conflict(new { message = "Franchise Code already exists" });

                var franchise = dto.ToModel();
                var createdFranchise = await _franchiseRepository.CreateAsync(franchise);
                return CreatedAtAction(nameof(GetById), new { id = createdFranchise.FranchiseId }, new FranchiseResponseDto(createdFranchise));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating franchise");
                return StatusCode(500, new { message = "An error occurred while creating the franchise" });
            }
        }
    }
}



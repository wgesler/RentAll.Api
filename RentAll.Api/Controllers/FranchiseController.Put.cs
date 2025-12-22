using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Franchises;

namespace RentAll.Api.Controllers
{
    public partial class FranchiseController
    {
        /// <summary>
        /// Update an existing franchise
        /// </summary>
        /// <param name="id">Franchise ID</param>
        /// <param name="dto">Franchise data</param>
        /// <returns>Updated franchise</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FranchiseUpdateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Franchise data is required" });

            if (id != dto.FranchiseId)
                return BadRequest(new { message = "Franchise ID mismatch" });

            if (string.IsNullOrWhiteSpace(dto.FranchiseCode))
                return BadRequest(new { message = "Franchise Code is required" });

            try
            {
                var existingFranchise = await _franchiseRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (existingFranchise == null)
                    return NotFound(new { message = "Franchise not found" });

                if (existingFranchise.FranchiseCode != dto.FranchiseCode)
                {
                    if (await _franchiseRepository.ExistsByFranchiseCodeAsync(dto.FranchiseCode, CurrentOrganizationId))
                        return Conflict(new { message = "Franchise Code already exists" });
                }

                var franchise = dto.ToModel();
                var updatedFranchise = await _franchiseRepository.UpdateByIdAsync(franchise);
                return Ok(new FranchiseResponseDto(updatedFranchise));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating franchise: {FranchiseId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the franchise" });
            }
        }
    }
}





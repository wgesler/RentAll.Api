using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Rentals;

namespace RentAll.Api.Controllers
{
    public partial class RentalController
    {
        /// <summary>
        /// Update an existing rental
        /// </summary>
        /// <param name="id">Rental ID</param>
        /// <param name="dto">Rental data</param>
        /// <returns>Updated rental</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRentalDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Rental data is required" });

            var (isValid, errorMessage) = dto.IsValid(id);
            if (!isValid)
                return BadRequest(new { message = errorMessage });

            try
            {
                // Check if rental exists
                var existingRental = await _rentalRepository.GetByIdAsync(id);
                if (existingRental == null)
                    return NotFound(new { message = "Rental not found" });

                var rental = dto.ToModel(existingRental, CurrentUser);
                var updatedRental = await _rentalRepository.UpdateByIdAsync(rental);
                return Ok(new RentalResponseDto(updatedRental));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rental: {RentalId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the rental" });
            }
        }
    }
}
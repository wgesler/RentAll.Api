using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Reservations;

namespace RentAll.Api.Controllers
{
    public partial class ReservationController  
    {
        /// <summary>
        /// Update an existing reservation
        /// </summary>
        /// <param name="id">Reservation ID</param>
        /// <param name="dto">Reservation data</param>
        /// <returns>Updated reservation</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReservationDto dto)
        {
            if (dto == null)
                return BadRequest("Reservation data is required");

            var (isValid, errorMessage) = dto.IsValid(id);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingReservation = await _reservationRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (existingReservation == null)
                    return NotFound("Reservation not found");

                var reservation = dto.ToModel(CurrentUser);
                var updatedReservation = await _reservationRepository.UpdateByIdAsync(reservation);
                return Ok(new ReservationResponseDto(updatedReservation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reservation: {ReservationId}", id);
                return ServerError("An error occurred while updating the reservation");
            }
        }
    }
}



using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Reservations;

namespace RentAll.Api.Controllers
{
    public partial class ReservationController
    {
        /// <summary>
        /// Create a new reservation
        /// </summary>
        /// <param name="dto">Reservation data</param>
        /// <returns>Created reservation</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Reservation data is required" });

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(new { message = errorMessage });

            try
            {
                var reservation = dto.ToModel(CurrentUser);
                var createdReservation = await _reservationRepository.CreateAsync(reservation);
                return CreatedAtAction(nameof(GetById), new { id = createdReservation.ReservationId }, new ReservationResponseDto(createdReservation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation");
                return StatusCode(500, new { message = "An error occurred while creating the reservation" });
            }
        }
    }
}



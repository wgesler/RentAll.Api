using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Reservations;
using RentAll.Domain.Enums;

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
                return BadRequest("Reservation data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
				// Get a new Contact code
				var code = await _organizationManager.GenerateEntityCodeAsync(dto.OrganizationId, EntityType.Reservation);				
                var reservation = dto.ToModel(code, CurrentUser);

                var createdReservation = await _reservationRepository.CreateAsync(reservation);
                return CreatedAtAction(nameof(GetById), new { id = createdReservation.ReservationId }, new ReservationResponseDto(createdReservation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation");
                return ServerError("An error occurred while creating the reservation");
            }
        }
    }
}



using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Reservations;

namespace RentAll.Api.Controllers
{
    public partial class ReservationController
    {
        /// <summary>
        /// Update an existing reservation
        /// </summary>
        /// <param name="dto">Reservation data</param>
        /// <returns>Updated reservation</returns>
        [HttpPut()]
        public async Task<IActionResult> Update([FromBody] UpdateReservationDto dto)
        {
            if (dto == null)
                return BadRequest("Reservation data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingReservation = await _reservationRepository.GetByIdAsync(dto.ReservationId, CurrentOrganizationId);
                if (existingReservation == null)
                    return NotFound("Reservation not found");

                var reservation = dto.ToModel(CurrentUser);
                var updatedReservation = await _reservationRepository.UpdateByIdAsync(reservation);
                return Ok(new ReservationResponseDto(updatedReservation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reservation: {ReservationId}", dto.ReservationId);
                return ServerError("An error occurred while updating the reservation");
            }
        }

        /// <summary>
        /// Update an existing reservation
        /// </summary>
        /// <param name="dto">Reservation data</param>
        /// <returns>Updated reservation</returns>
        [HttpPut("payment")]
		public async Task<IActionResult> ApplyPayment([FromBody] ReservationPaymentDto dto)
		{
			if (dto == null)
				return BadRequest("Reservation data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				var existingReservation = await _reservationRepository.GetByIdAsync(dto.ReservationId, CurrentOrganizationId);
				if (existingReservation == null)
					return NotFound("Reservation not found");

                await _accountingManager.ApplyPaymentToReservationAsync(dto.ReservationId, CurrentOrganizationId, CurrentOfficeAccess, 
                    dto.CostCodeId, dto.Description, dto.Amount, CurrentUser);
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating reservation: {ReservationId}", dto.ReservationId);
				return ServerError("An error occurred while updating the reservation");
			}
		}
	}
}



using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Reservations;

namespace RentAll.Api.Controllers
{
    public partial class ReservationController
    {
		/// <summary>
		/// Get all reservations
		/// </summary>
		/// <returns>List of reservations</returns>
		[HttpGet("")]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var reservations = await _reservationRepository.GetAllAsync(CurrentOrganizationId);
				var response = reservations.Select(r => new ReservationResponseDto(r));
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting reservations");
				return StatusCode(500, new { message = "An error occurred while retrieving reservations" });
			}
		}

		/// <summary>
		/// Get active reservations
		/// </summary>
		/// <returns>List of active reservations</returns>
		[HttpGet("active")]
		public async Task<IActionResult> GetActiveRentals()
		{
			try
			{
				var reservations = await _reservationRepository.GetActiveReservationsAsync(CurrentOrganizationId);
				var response = reservations.Select(r => new ReservationResponseDto(r));
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting active reservations");
				return StatusCode(500, new { message = "An error occurred while retrieving active reservations" });
			}
		}

		/// <summary>
		/// Get reservation by ID
		/// </summary>
		/// <param name="id">Reservation ID</param>
		/// <returns>Reservation</returns>
		[HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { message = "Reservation ID is required" });

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound(new { message = "Reservation not found" });

                return Ok(new ReservationResponseDto(reservation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation by ID: {ReservationId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the reservation" });
            }
        }

        /// <summary>
        /// Get reservations by Property ID
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>List of reservations</returns>
        [HttpGet("property/{propertyId}")]
        public async Task<IActionResult> GetByPropertyId(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest(new { message = "Property ID is required" });

            try
            {
                var reservations = await _reservationRepository.GetByPropertyIdAsync(propertyId, CurrentOrganizationId);
                var response = reservations.Select(r => new ReservationResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations by PropertyId: {PropertyId}", propertyId);
                return StatusCode(500, new { message = "An error occurred while retrieving reservations" });
            }
        }

        /// <summary>
        /// Get reservations by Contact ID
        /// </summary>
        /// <param name="contactId">Contact ID</param>
        /// <returns>List of reservations</returns>
        [HttpGet("contact/{contactId}")]
        public async Task<IActionResult> GetByContactId(Guid contactId)
        {
            if (contactId == Guid.Empty)
                return BadRequest(new { message = "Contact ID is required" });

            try
            {
                var reservations = await _reservationRepository.GetByClientIdAsync(contactId, CurrentOrganizationId);
                var response = reservations.Select(r => new ReservationResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations by ContactId: {ContactId}", contactId);
                return StatusCode(500, new { message = "An error occurred while retrieving reservations" });
            }
        }
    }
}



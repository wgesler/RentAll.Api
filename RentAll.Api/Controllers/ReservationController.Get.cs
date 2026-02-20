using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Reservations.Reservations;

namespace RentAll.Api.Controllers
{
    public partial class ReservationController
    {
        /// <summary>
        /// Get all reservations
        /// </summary>
        /// <returns>List of reservations</returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetList()
        {
            try
            {
                var list = await _reservationRepository.GetListByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = list.Select(r => new ReservationListResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations");
                return ServerError("An error occurred while retrieving reservations");
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
                return BadRequest("Reservation ID is required");

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                return Ok(new ReservationResponseDto(reservation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation by ID: {ReservationId}", id);
                return ServerError("An error occurred while retrieving the reservation");
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
                return BadRequest("Property ID is required");

            try
            {
                var reservations = await _reservationRepository.GetByPropertyIdAsync(propertyId, CurrentOrganizationId);
                var response = reservations.Select(r => new ReservationResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations by PropertyId: {PropertyId}", propertyId);
                return ServerError("An error occurred while retrieving reservations");
            }
        }
    }
}



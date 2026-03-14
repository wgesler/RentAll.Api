
namespace RentAll.Api.Controllers
{
    public partial class ReservationController
    {

        #region Get

        [HttpGet("list")]
        public async Task<IActionResult> GetReservationListByOfficeIdAsync()
        {
            try
            {
                var list = await _reservationRepository.GetReservationListByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = list.Select(r => new ReservationListResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations");
                return ServerError("An error occurred while retrieving reservations");
            }
        }

        [HttpGet("property/{propertyId}")]
        public async Task<IActionResult> GetReservationListByPropertyIdAsync(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                var reservations = await _reservationRepository.GetReservationListByPropertyIdAsync(propertyId, CurrentOrganizationId);
                var response = reservations.Select(r => new ReservationResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations by PropertyId: {PropertyId}", propertyId);
                return ServerError("An error occurred while retrieving reservations");
            }
        }

        [HttpGet("{reservationId}")]
        public async Task<IActionResult> GetReservationByIdAsync(Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                return BadRequest("Reservation ID is required");

            try
            {
                var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                return Ok(new ReservationResponseDto(reservation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation by ID: {ReservationId}", reservationId);
                return ServerError("An error occurred while retrieving the reservation");
            }
        }
        #endregion

        #region Post
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

                var response = new ReservationResponseDto(createdReservation);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation");
                return ServerError("An error occurred while creating the reservation");
            }
        }
        #endregion

        #region Put
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateReservationDto dto)
        {
            if (dto == null)
                return BadRequest("Reservation data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingReservation = await _reservationRepository.GetReservationByIdAsync(dto.ReservationId, CurrentOrganizationId);
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
        #endregion

        #region Delete
        [HttpDelete("{reservationId}")]
        public async Task<IActionResult> DeleteReservationByIdAsync(Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                return BadRequest("Reservation ID is required");

            try
            {
                await _reservationRepository.DeleteReservationByIdAsync(reservationId, CurrentOrganizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reservation: {ReservationId}", reservationId);
                return ServerError("An error occurred while deleting the reservation");
            }
        }

        #endregion
    }
}

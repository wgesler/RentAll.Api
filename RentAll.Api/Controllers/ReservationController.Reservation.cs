
namespace RentAll.Api.Controllers
{
    public partial class ReservationController
    {

        #region Get

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
        /// <param name="reservationId">Reservation ID</param>
        /// <returns>Reservation</returns>
        [HttpGet("{reservationId}")]
        public async Task<IActionResult> GetById(Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                return BadRequest("Reservation ID is required");

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(reservationId, CurrentOrganizationId);
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

        #endregion

        #region Post

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
                return CreatedAtAction(nameof(GetById), new { reservationId = createdReservation.ReservationId }, new ReservationResponseDto(createdReservation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation");
                return ServerError("An error occurred while creating the reservation");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing reservation
        /// </summary>
        /// <param name="dto">Reservation data</param>
        /// <returns>Updated reservation</returns>
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

        #endregion

        #region Delete

        /// <summary>
        /// Delete a reservation
        /// </summary>
        /// <param name="reservationId">Reservation ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{reservationId}")]
        public async Task<IActionResult> Delete(Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                return BadRequest("Reservation ID is required");

            try
            {
                // Check if reservation exists
                var reservation = await _reservationRepository.GetByIdAsync(reservationId, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                await _reservationRepository.DeleteByIdAsync(reservationId);
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

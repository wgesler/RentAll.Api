namespace RentAll.Api.Controllers
{
    public partial class ReservationController
    {
        #region Get
        [HttpGet("tracker-response/reservation/{reservationId}")]
        public async Task<IActionResult> GetTrackerResponsesByReservationId(Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                return BadRequest("ReservationId is required");

            try
            {
                var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                var responses = await _reservationRepository.GetTrackerResponsesByReservationIdAsync(reservationId);
                var response = responses.Select(r => new ReservationTrackerResponseResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracker responses by ReservationId: {ReservationId}", reservationId);
                return ServerError("An error occurred while retrieving tracker responses");
            }
        }

        [HttpGet("tracker-response-option/reservation/{reservationId}")]
        public async Task<IActionResult> GetTrackerResponseOptionsByReservationId(Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                return BadRequest("ReservationId is required");

            try
            {
                var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                var options = await _reservationRepository.GetTrackerResponseOptionsByReservationIdAsync(reservationId);
                var response = options.Select(o => new ReservationTrackerResponseOptionResponseDto(o));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracker response options by ReservationId: {ReservationId}", reservationId);
                return ServerError("An error occurred while retrieving tracker response options");
            }
        }
        #endregion

        #region Post
        [HttpPost("tracker-response")]
        public async Task<IActionResult> CreateTrackerResponse([FromBody] ReservationTrackerResponseCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker response data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var reservation = await _reservationRepository.GetReservationByIdAsync(dto.ReservationId, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                var trackerResponse = new TrackerResponse
                {
                    TrackerDefinitionId = dto.TrackerDefinitionId,
                    PropertyId = reservation.PropertyId,
                    ReservationId = dto.ReservationId,
                    EntityTypeId = (int)EntityType.Reservation,
                    EntityId = dto.ReservationId,
                    IsChecked = dto.IsChecked,
                    CheckedOn = dto.CheckedOn,
                    CheckedBy = dto.CheckedBy,
                    CreatedBy = CurrentUser
                };

                var created = await _reservationRepository.CreateTrackerResponseAsync(trackerResponse);
                return Ok(new ReservationTrackerResponseResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tracker response for ReservationId: {ReservationId}", dto.ReservationId);
                return ServerError("An error occurred while creating tracker response");
            }
        }

        [HttpPost("tracker-response-option")]
        public async Task<IActionResult> CreateTrackerResponseOption([FromBody] ReservationTrackerResponseOptionCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker response option data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingResponse = await _reservationRepository.GetTrackerResponseByIdAsync(dto.TrackerResponseId);
                if (existingResponse == null)
                    return NotFound("Tracker response not found");

                if (!existingResponse.ReservationId.HasValue)
                    return BadRequest("Tracker response is not reservation-scoped");

                var reservation = await _reservationRepository.GetReservationByIdAsync(existingResponse.ReservationId.Value, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                var trackerResponseOption = dto.ToModel(CurrentUser);
                var created = await _reservationRepository.CreateTrackerResponseOptionAsync(trackerResponseOption);
                return Ok(new ReservationTrackerResponseOptionResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tracker response option for TrackerResponseId: {TrackerResponseId}", dto.TrackerResponseId);
                return ServerError("An error occurred while creating tracker response option");
            }
        }
        #endregion

        #region Put
        [HttpPut("tracker-response")]
        public async Task<IActionResult> UpdateTrackerResponse([FromBody] ReservationTrackerResponseUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker response data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var reservation = await _reservationRepository.GetReservationByIdAsync(dto.ReservationId, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                var existing = await _reservationRepository.GetTrackerResponseByIdAsync(dto.TrackerResponseId);
                if (existing == null)
                    return NotFound("Tracker response not found");

                var trackerResponse = new TrackerResponse
                {
                    TrackerResponseId = dto.TrackerResponseId,
                    TrackerDefinitionId = dto.TrackerDefinitionId,
                    PropertyId = reservation.PropertyId,
                    ReservationId = dto.ReservationId,
                    EntityTypeId = (int)EntityType.Reservation,
                    EntityId = dto.ReservationId,
                    IsChecked = dto.IsChecked,
                    CheckedOn = dto.CheckedOn,
                    CheckedBy = dto.CheckedBy,
                    ModifiedBy = CurrentUser
                };

                var updated = await _reservationRepository.UpdateTrackerResponseByIdAsync(trackerResponse);
                return Ok(new ReservationTrackerResponseResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tracker response: {TrackerResponseId}", dto.TrackerResponseId);
                return ServerError("An error occurred while updating tracker response");
            }
        }

        [HttpPut("tracker-response-option")]
        public async Task<IActionResult> UpdateTrackerResponseOption([FromBody] ReservationTrackerResponseOptionUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker response option data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingResponse = await _reservationRepository.GetTrackerResponseByIdAsync(dto.TrackerResponseId);
                if (existingResponse == null)
                    return NotFound("Tracker response not found");

                if (!existingResponse.ReservationId.HasValue)
                    return BadRequest("Tracker response is not reservation-scoped");

                var reservation = await _reservationRepository.GetReservationByIdAsync(existingResponse.ReservationId.Value, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                var updated = await _reservationRepository.UpdateTrackerResponseOptionByIdAsync(dto.TrackerResponseId, dto.TrackerDefinitionOptionId, dto.NewTrackerDefinitionOptionId);
                return Ok(new ReservationTrackerResponseOptionResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tracker response option for TrackerResponseId: {TrackerResponseId}", dto.TrackerResponseId);
                return ServerError("An error occurred while updating tracker response option");
            }
        }
        #endregion

        #region Delete
        [HttpDelete("tracker-response/{trackerResponseId:guid}")]
        public async Task<IActionResult> DeleteTrackerResponseById(Guid trackerResponseId)
        {
            if (trackerResponseId == Guid.Empty)
                return BadRequest("TrackerResponseId is required");

            try
            {
                var existing = await _reservationRepository.GetTrackerResponseByIdAsync(trackerResponseId);
                if (existing == null)
                    return NotFound("Tracker response not found");

                if (!existing.ReservationId.HasValue)
                    return BadRequest("Tracker response is not reservation-scoped");

                var reservation = await _reservationRepository.GetReservationByIdAsync(existing.ReservationId.Value, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                await _reservationRepository.DeleteTrackerResponseByIdAsync(trackerResponseId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tracker response: {TrackerResponseId}", trackerResponseId);
                return ServerError("An error occurred while deleting tracker response");
            }
        }

        [HttpDelete("tracker-response-option/{trackerResponseId:guid}/{trackerDefinitionOptionId:guid}")]
        public async Task<IActionResult> DeleteTrackerResponseOptionById(Guid trackerResponseId, Guid trackerDefinitionOptionId)
        {
            if (trackerResponseId == Guid.Empty)
                return BadRequest("TrackerResponseId is required");

            if (trackerDefinitionOptionId == Guid.Empty)
                return BadRequest("TrackerDefinitionOptionId is required");

            try
            {
                var existingResponse = await _reservationRepository.GetTrackerResponseByIdAsync(trackerResponseId);
                if (existingResponse == null)
                    return NotFound("Tracker response not found");

                if (!existingResponse.ReservationId.HasValue)
                    return BadRequest("Tracker response is not reservation-scoped");

                var reservation = await _reservationRepository.GetReservationByIdAsync(existingResponse.ReservationId.Value, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                await _reservationRepository.DeleteTrackerResponseOptionByIdAsync(trackerResponseId, trackerDefinitionOptionId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tracker response option for TrackerResponseId: {TrackerResponseId}", trackerResponseId);
                return ServerError("An error occurred while deleting tracker response option");
            }
        }
        #endregion
    }
}

namespace RentAll.Api.Controllers;

using RentAll.Infrastructure.Repositories.Reservations;

public partial class ReservationController
{
    #region Get

    [HttpGet("payment/reservation/{reservationId}")]
    public async Task<IActionResult> GetReservationPaymentsByReservationId(Guid reservationId)
    {
        if (reservationId == Guid.Empty)
            return BadRequest("ReservationId is required");

        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");

            var payments = await _reservationRepository.GetReservationPaymentsByReservationIdAsync(CurrentOrganizationId, reservationId);
            return Ok(payments.Select(p => new ReservationPaymentResponseDto(p)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservation payments by ReservationId: {ReservationId}", reservationId);
            return ServerError("An error occurred while retrieving reservation payments");
        }
    }

    #endregion

    #region Post

    [HttpPost("payment")]
    public async Task<IActionResult> CreateReservationPayment([FromBody] CreateReservationPaymentDto dto)
    {
        if (dto == null)
            return BadRequest("Reservation payment data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(dto.ReservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");

            var windowValidation = ReservationRepository.ValidateReservationPaymentWindow(reservation, dto.StartDate, dto.EndDate);
            if (!windowValidation.IsValid)
                return BadRequest(windowValidation.ErrorMessage ?? "Invalid request data");

            var existing = await _reservationRepository.GetReservationPaymentsByReservationIdAsync(CurrentOrganizationId, dto.ReservationId);
            var overlapValidation = ReservationRepository.ValidateReservationPaymentOverlap(existing, dto.StartDate, dto.EndDate);
            if (!overlapValidation.IsValid)
                return BadRequest(overlapValidation.ErrorMessage ?? "Invalid request data");

            var amountValidation = ReservationRepository.ValidateReservationPaymentAmount(dto.Amount);
            if (!amountValidation.IsValid)
                return BadRequest(amountValidation.ErrorMessage ?? "Invalid request data");

            var created = await _reservationRepository.CreateReservationPaymentAsync(dto.ToModel(CurrentOrganizationId, CurrentUser));
            return Ok(new ReservationPaymentResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reservation payment");
            return ServerError("An error occurred while creating the reservation payment");
        }
    }

    [HttpPost("payment/apply-rent-change")]
    public async Task<IActionResult> ApplyReservationRentChange([FromBody] ApplyReservationRentChangeDto dto)
    {
        if (dto == null)
            return BadRequest("Rent change data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(dto.ReservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");

            var payments = await _reservationRepository.ApplyReservationRentChangeAsync(reservation, dto.NewAmount, dto.EffectiveDate, CurrentUser);
            return Ok(payments.Select(p => new ReservationPaymentResponseDto(p)));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying reservation rent change: {ReservationId}", dto.ReservationId);
            return ServerError("An error occurred while applying the rent change");
        }
    }

    #endregion

    #region Put

    [HttpPut("payment")]
    public async Task<IActionResult> UpdateReservationPayment([FromBody] UpdateReservationPaymentDto dto)
    {
        if (dto == null)
            return BadRequest("Reservation payment data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(dto.ReservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");

            var existingPayment = await _reservationRepository.GetReservationPaymentByIdAsync(dto.ReservationPaymentId, CurrentOrganizationId);
            if (existingPayment == null)
                return NotFound("Reservation payment not found");

            var windowValidation = ReservationRepository.ValidateReservationPaymentWindow(reservation, dto.StartDate, dto.EndDate);
            if (!windowValidation.IsValid)
                return BadRequest(windowValidation.ErrorMessage ?? "Invalid request data");

            var existing = await _reservationRepository.GetReservationPaymentsByReservationIdAsync(CurrentOrganizationId, dto.ReservationId);
            var overlapValidation = ReservationRepository.ValidateReservationPaymentOverlap(
                existing,
                dto.StartDate,
                dto.EndDate,
                dto.ReservationPaymentId);
            if (!overlapValidation.IsValid)
                return BadRequest(overlapValidation.ErrorMessage ?? "Invalid request data");

            var amountValidation = ReservationRepository.ValidateReservationPaymentAmount(dto.Amount);
            if (!amountValidation.IsValid)
                return BadRequest(amountValidation.ErrorMessage ?? "Invalid request data");

            var updated = await _reservationRepository.UpdateReservationPaymentByIdAsync(dto.ToModel(CurrentOrganizationId, CurrentUser));
            return Ok(new ReservationPaymentResponseDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation payment: {ReservationPaymentId}", dto.ReservationPaymentId);
            return ServerError("An error occurred while updating the reservation payment");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("payment/{reservationPaymentId}")]
    public async Task<IActionResult> DeleteReservationPaymentById(int reservationPaymentId)
    {
        if (reservationPaymentId <= 0)
            return BadRequest("ReservationPaymentId is required");

        try
        {
            var existingPayment = await _reservationRepository.GetReservationPaymentByIdAsync(reservationPaymentId, CurrentOrganizationId);
            if (existingPayment == null)
                return NotFound("Reservation payment not found");

            await _reservationRepository.DeleteReservationPaymentByIdAsync(reservationPaymentId, CurrentOrganizationId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reservation payment: {ReservationPaymentId}", reservationPaymentId);
            return ServerError("An error occurred while deleting the reservation payment");
        }
    }

    #endregion
}

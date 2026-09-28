using RentAll.Api.Dtos.Reservations.Billed;
namespace RentAll.Api.Controllers;

public partial class ReservationController
{
    #region Billed Get

    [HttpPost("billed/search")]
    public async Task<IActionResult> SearchBilledMatchup([FromBody] GetBilledMatchupDto dto)
    {
        if (dto == null)
            return BadRequest("Billed search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var rows = await _accountingManager.GetBilledMatchupAsync(CurrentOrganizationId, dto.ResolvedOfficeIds);
            return Ok(rows.Select(row => new BilledResponseDto(row)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching billed matchup rows");
            return ServerError("An error occurred while retrieving billed matchup rows");
        }
    }

    [HttpGet("billed/reservation/{reservationId:guid}")]
    public async Task<IActionResult> GetBilledByReservationId(Guid reservationId)
    {
        if (reservationId == Guid.Empty)
            return BadRequest("ReservationId is required");

        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");

            var billedRows = await _accountingManager.GetBilledByReservationIdAsync(CurrentOrganizationId, reservationId);
            if (billedRows.Count == 0)
                return NotFound("Billed matchup rows not found");

            return Ok(billedRows.Select(row => new BilledResponseDto(row)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billed matchup by ReservationId: {ReservationId}", reservationId);
            return ServerError("An error occurred while retrieving the billed matchup row");
        }
    }

    #endregion

    #region Billed Post

    [HttpPost("billed")]
    public async Task<IActionResult> CreateBilled([FromBody] CreateBilledDto dto)
    {
        if (dto == null)
            return BadRequest("Billed data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(dto.ReservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");

            var created = await _accountingManager.CreateBilledAsync(dto.ToModel(CurrentOrganizationId, CurrentUser), CurrentUser);
            return Ok(new BilledResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating billed matchup row");
            return ServerError("An error occurred while creating the billed matchup row");
        }
    }

    [HttpPost("billed/rebuild")]
    public async Task<IActionResult> RebuildBilledMatchup([FromBody] GetBilledMatchupDto dto)
    {
        if (dto == null)
            return BadRequest("Billed rebuild criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            await _accountingManager.RebuildReservationBilledMatchupAsync(CurrentOrganizationId, dto.ResolvedOfficeIds, CurrentUser);
            var rows = await _accountingManager.GetBilledMatchupAsync(CurrentOrganizationId, dto.ResolvedOfficeIds);
            return Ok(rows.Select(row => new BilledResponseDto(row)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding billed matchup rows");
            return ServerError("An error occurred while rebuilding billed matchup rows");
        }
    }

    #endregion

    #region Billed Put

    [HttpPut("billed/reservation/{reservationId:guid}")]
    public async Task<IActionResult> UpdateBilledByReservationId(Guid reservationId, [FromBody] UpdateBilledDto dto)
    {
        if (reservationId == Guid.Empty)
            return BadRequest("ReservationId is required");
        if (dto == null)
            return BadRequest("Billed data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");

            var existing = await _accountingManager.GetBilledByReservationIdAsync(CurrentOrganizationId, reservationId);
            if (existing.Count == 0)
                return NotFound("Billed matchup rows not found");
            if (existing.All(row => row.MonthStart != dto.MonthStart))
                return NotFound("Billed matchup row not found for month");

            var updated = await _accountingManager.UpdateBilledByReservationIdAsync(
                dto.ToModel(CurrentOrganizationId, reservationId, CurrentUser),
                CurrentUser);
            if (updated == null)
                return NotFound("Billed matchup row not found");

            return Ok(new BilledResponseDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating billed matchup by ReservationId: {ReservationId}", reservationId);
            return ServerError("An error occurred while updating the billed matchup row");
        }
    }

    [HttpPut("billed/{billedId:int}/ignore")]
    public async Task<IActionResult> SetBilledIgnoreById(int billedId, [FromBody] SetBilledIgnoreDto dto)
    {
        if (billedId <= 0)
            return BadRequest("BilledId is required");
        if (dto == null)
            return BadRequest("Ignore data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var updated = await _accountingManager.SetBilledIgnoreByIdAsync(
                CurrentOrganizationId,
                billedId,
                dto.Ignore,
                CurrentUser);
            if (updated == null)
                return NotFound("Billed row not found");

            return Ok(new BilledResponseDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting billed ignore for BilledId: {BilledId}", billedId);
            return ServerError("An error occurred while updating billed ignore");
        }
    }

    #endregion

    #region Billed Delete

    [HttpDelete("billed/reservation/{reservationId:guid}")]
    public async Task<IActionResult> DeleteBilledByReservationId(Guid reservationId)
    {
        if (reservationId == Guid.Empty)
            return BadRequest("ReservationId is required");

        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");

            var existing = await _accountingManager.GetBilledByReservationIdAsync(CurrentOrganizationId, reservationId);
            if (existing.Count == 0)
                return NotFound("Billed matchup rows not found");

            await _accountingManager.DeleteBilledByReservationIdAsync(CurrentOrganizationId, reservationId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting billed matchup by ReservationId: {ReservationId}", reservationId);
            return ServerError("An error occurred while deleting the billed matchup row");
        }
    }

    #endregion
}

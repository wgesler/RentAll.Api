namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    #region Get

    [HttpGet("security-deposit/unreturned")]
    public async Task<IActionResult> GetUnreturnedSecurityDepositsAsync([FromQuery] int? officeId = null)
    {
        try
        {
            var result = await _accountingManager.GetUnreturnedSecurityDepositsAsync(
                CurrentOrganizationId,
                CurrentOfficeAccess,
                officeId);
            return Ok(new UnreturnedSecurityDepositsResponseDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unreturned security deposits");
            return ServerError("An error occurred while retrieving unreturned security deposits");
        }
    }

    [HttpGet("security-deposit/{reservationId:guid}/detail")]
    public async Task<IActionResult> GetSecurityDepositDetailAsync(Guid reservationId)
    {
        try
        {
            var result = await _accountingManager.GetSecurityDepositDetailAsync(
                reservationId,
                CurrentOrganizationId,
                CurrentOfficeAccess);
            return Ok(new SecurityDepositDetailResponseDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security deposit detail for reservation: {ReservationId}", reservationId);
            return ServerError(ex.Message);
        }
    }

    #endregion

    #region Put

    [HttpPut("security-deposit/return")]
    public async Task<IActionResult> ApplySecurityDepositReturnAsync([FromBody] SecurityDepositReturnRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Security deposit return data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var updatedReservation = await _accountingManager.ApplySecurityDepositReturnAsync(
                dto.ReservationId,
                CurrentOrganizationId,
                CurrentOfficeAccess,
                dto.ChartOfAccountId,
                dto.Description,
                dto.Amount,
                dto.PaymentDate,
                (PaymentType)dto.PaymentTypeId,
                CurrentUser);

            return Ok(new ReservationResponseDto(updatedReservation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning security deposit for reservation: {ReservationId}", dto.ReservationId);
            return ServerError("An error occurred while returning the security deposit");
        }
    }

    [HttpPut("security-deposit/transfer")]
    public async Task<IActionResult> ApplySecurityDepositTransferAsync([FromBody] SecurityDepositReturnRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Security deposit transfer data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var updatedReservation = await _accountingManager.ApplySecurityDepositTransferAsync(
                dto.ReservationId,
                CurrentOrganizationId,
                CurrentOfficeAccess,
                dto.ChartOfAccountId,
                dto.Description,
                dto.Amount,
                dto.PaymentDate,
                (PaymentType)dto.PaymentTypeId,
                CurrentUser);

            return Ok(new ReservationResponseDto(updatedReservation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring security deposit for reservation: {ReservationId}", dto.ReservationId);
            return ServerError(ex.Message);
        }
    }

    #endregion
}

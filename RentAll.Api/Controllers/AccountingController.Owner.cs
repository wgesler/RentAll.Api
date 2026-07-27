using RentAll.Api.Dtos.Accounting.Owners;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    [HttpPut("owner/payment")]
    public async Task<IActionResult> ApplyOwnerPayment([FromBody] OwnerPaymentsRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Owner payment data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var journalEntries = await _accountingManager.ApplyPaymentToOwnersAsync(dto.ToModel(), CurrentOrganizationId, CurrentOfficeAccess, CurrentUser);
            return Ok(new OwnerPaymentResponseDto(journalEntries));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying owner payments");
            return ServerError("An error occurred while applying owner payments");
        }
    }
}

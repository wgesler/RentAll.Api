using RentAll.Api.Dtos.Accounting.Owners;
using RentAll.Domain.Models;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    [HttpPut("owner/payment")]
    public async Task<IActionResult> ApplyOwnerPayment([FromBody] OwnerPaymentRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Owner payment data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var ownerPayment = await _accountingManager.ApplyPaymentToOwnersAsync(dto.Lines.Select(line => new OwnerPaymentLine(line.OfficeId, line.OwnerId, line.PropertyId, line.Amount)).ToList(), CurrentOrganizationId, CurrentOfficeAccess, dto.ChartOfAccountId, dto.Description, dto.PaymentDate, (PaymentType)dto.PaymentTypeId, CurrentUser);
            var journalEntries = await _accountingManager.CreateJournalEntriesFromOwnerPaymentAsync(ownerPayment, CurrentUser);
            var response = new OwnerPaymentResponseDto(journalEntries);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying owner payments");
            return ServerError("An error occurred while applying owner payments");
        }
    }
}

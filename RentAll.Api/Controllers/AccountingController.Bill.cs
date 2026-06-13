using RentAll.Api.Dtos.Accounting.Bills;

namespace RentAll.Api.Controllers
{
    public partial class AccountingController
    {
        [HttpPut("bill/payment")]
        public async Task<IActionResult> ApplyBillPayment([FromBody] BillPaymentRequestDto dto)
        {
            if (dto == null)
                return BadRequest("Bill payment data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var billPayment = await _accountingManager.ApplyPaymentToBillsAsync(dto.Bills, CurrentOrganizationId, CurrentOfficeAccess,
                    dto.CostCodeId, dto.Description, dto.Amount, dto.PaymentDate, CurrentUser);

                try
                {
                    await _accountingManager.CreateJournalEntriesFromBillPaymentAsync(billPayment, CurrentUser);
                }
                catch (Exception journalEntryEx)
                {
                    _logger.LogError(journalEntryEx, "Bill payment was applied but journal entry creation failed");
                    return BadRequest($"Bill payment was applied but general ledger entry creation failed: {journalEntryEx.Message}");
                }

                var response = new BillPaymentResponseDto(billPayment);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying payments to bills");
                return ServerError("An error occurred while applying payments to bills");
            }
        }
    }
}

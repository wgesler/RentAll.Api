using RentAll.Api.Dtos.Accounting.Deposits;

namespace RentAll.Api.Controllers
{
    public partial class AccountingController
    {
        [HttpPut("deposit")]
        public async Task<IActionResult> MakeDeposit([FromBody] DepositRequestDto dto)
        {
            if (dto == null)
                return BadRequest("Deposit data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var journalEntry = await _accountingManager.CreateJournalEntryFromDepositAsync(
                    dto.OfficeId,
                    CurrentOrganizationId,
                    dto.ChartOfAccountId,
                    dto.Description,
                    dto.Amount,
                    dto.DepositDate,
                    dto.JournalEntryLineIds,
                    CurrentUser);

                if (journalEntry == null)
                    return NotFound(new { message = "Accounting is not enabled in this environment." });

                return Ok(new DepositResponseDto(journalEntry));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deposit journal entry");
                return BadRequest(ex.Message);
            }
        }
    }
}

using RentAll.Api.Dtos.Accounting.Deposits;
using RentAll.Domain.Configuration;

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

            if (!await _featureFlagService.IsEnabledAsync(FeatureFlagKeys.Accounting, CurrentOrganizationId))
                return NotFound(new { message = "Accounting is not enabled in this environment." });

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

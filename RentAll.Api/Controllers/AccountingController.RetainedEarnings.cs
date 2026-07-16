using RentAll.Api.Dtos.Accounting.JournalEntries;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    [HttpPost("retained-earnings/journal-entry/preview")]
    public async Task<IActionResult> PreviewRetainedEarningsJournalEntry([FromBody] CreateRetainedEarningsJournalEntryRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Retained earnings request is required.");

        var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid retained earnings request.");

        try
        {
            var journalEntry = await _accountingManager.PreviewRetainedEarningsJournalEntryForFiscalYearEndAsync(
                CurrentOrganizationId,
                dto.OfficeId,
                dto.FiscalYearEndYear);

            var response = new JournalEntryResponseDto(journalEntry);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing retained earnings journal entry for office {OfficeId}, fiscal year end {FiscalYearEndYear}", dto.OfficeId, dto.FiscalYearEndYear);
            return BadRequest(ex.Message);
        }
    }
}

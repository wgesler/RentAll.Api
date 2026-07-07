using RentAll.Api.Dtos.Accounting.JournalEntryRecap;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    [HttpPost("journal-entry-recap/search")]
    public async Task<IActionResult> SearchJournalEntryRecapLines([FromBody] GetJournalEntryRecapDto dto)
    {
        if (dto == null)
            return BadRequest("Journal entry recap search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var lines = await _journalEntryRepository.GetJournalEntryRecapLinesAsync(criteria);
            var response = lines.Select(line => new JournalEntryRecapLineResponseDto(line)).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching journal entry recap lines");
            return ServerError("An error occurred while retrieving journal entry recap lines");
        }
    }
}

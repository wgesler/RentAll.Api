using RentAll.Api.Dtos.Accounting.JournalEntries;

namespace RentAll.Api.Controllers
{
    public partial class AccountingController
    {
        #region Get

        [HttpPost("journal-entry/search")]
        public async Task<IActionResult> SearchJournalEntries([FromBody] GetJournalEntryDto dto)
        {
            if (dto == null)
                return BadRequest("Journal entry search criteria is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var criteria = dto.ToCriteria(CurrentOrganizationId);
                var journalEntries = await _journalEntryRepository.GetJournalEntriesAsync(criteria);
                var response = journalEntries.Select(j => new JournalEntryResponseDto(j)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching journal entries");
                return ServerError("An error occurred while retrieving journal entries");
            }
        }

        [HttpGet("journal-entry/{journalEntryId}")]
        public async Task<IActionResult> GetJournalEntryById(Guid journalEntryId)
        {
            if (journalEntryId == Guid.Empty)
                return BadRequest("Journal entry ID is required");

            try
            {
                var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, CurrentOrganizationId);
                if (journalEntry == null)
                    return NotFound("Journal entry not found");

                var response = new JournalEntryResponseDto(journalEntry);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting journal entry by ID: {JournalEntryId}", journalEntryId);
                return ServerError("An error occurred while retrieving the journal entry");
            }
        }

        #endregion

        #region Post

        [HttpPost("journal-entry")]
        public async Task<IActionResult> CreateJournalEntry([FromBody] CreateJournalEntryDto dto)
        {
            if (dto == null)
                return BadRequest("Journal entry data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid journal entry data");

            try
            {
                var journalEntry = dto.ToModel(CurrentUser);
                journalEntry.OrganizationId = CurrentOrganizationId;
                var createdJournalEntry = await _accountingManager.CreateJournalEntryAsync(journalEntry);

                var response = new JournalEntryResponseDto(createdJournalEntry);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating journal entry");
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Put

        [HttpPut("journal-entry")]
        public async Task<IActionResult> UpdateJournalEntry([FromBody] UpdateJournalEntryDto dto)
        {
            if (dto == null)
                return BadRequest("Journal entry data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid journal entry data");

            try
            {
                var existingJournalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(dto.JournalEntryId, CurrentOrganizationId);
                if (existingJournalEntry == null)
                    return NotFound("Journal entry not found");

                var journalEntry = dto.ToModel(CurrentUser);
                journalEntry.OrganizationId = CurrentOrganizationId;

                var updatedJournalEntry = await _accountingManager.UpdateJournalEntryAsync(journalEntry);

                var response = new JournalEntryResponseDto(updatedJournalEntry);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating journal entry: {JournalEntryId}", dto.JournalEntryId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("journal-entry/{journalEntryId}/post")]
        public async Task<IActionResult> PostJournalEntry(Guid journalEntryId)
        {
            if (journalEntryId == Guid.Empty)
                return BadRequest("Journal entry ID is required");

            try
            {
                var journalEntry = await _accountingManager.PostJournalEntryAsync(journalEntryId, CurrentOrganizationId, CurrentUser);
                var response = new JournalEntryResponseDto(journalEntry);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting journal entry: {JournalEntryId}", journalEntryId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("journal-entry/{journalEntryId}/void")]
        public async Task<IActionResult> VoidJournalEntry(Guid journalEntryId)
        {
            if (journalEntryId == Guid.Empty)
                return BadRequest("Journal entry ID is required");

            try
            {
                var journalEntry = await _accountingManager.VoidJournalEntryAsync(journalEntryId, CurrentOrganizationId, CurrentUser);
                var response = new JournalEntryResponseDto(journalEntry);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voiding journal entry: {JournalEntryId}", journalEntryId);
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Delete

        [HttpDelete("journal-entry/{journalEntryId}")]
        public async Task<IActionResult> DeleteJournalEntryById(Guid journalEntryId)
        {
            if (journalEntryId == Guid.Empty)
                return BadRequest("Journal entry ID is required");

            try
            {
                await _accountingManager.DeleteJournalEntryAsync(journalEntryId, CurrentOrganizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting journal entry: {JournalEntryId}", journalEntryId);
                return BadRequest(ex.Message);
            }
        }

        #endregion
    }
}

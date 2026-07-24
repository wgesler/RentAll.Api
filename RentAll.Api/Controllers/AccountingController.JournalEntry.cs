using RentAll.Api.Dtos.Accounting.ChartOfAccounts;
using RentAll.Api.Dtos.Accounting.JournalEntries;
using RentAll.Api.Dtos.Accounting.JournalEntryLines;

namespace RentAll.Api.Controllers
{
    public partial class AccountingController
    {
        #region Get

        [HttpPost("journal-entry-line/search")]
        public async Task<IActionResult> SearchJournalEntryLines([FromBody] GetJournalEntryLineDto dto)
        {
            if (dto == null)
                return BadRequest("Journal entry line search criteria is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var criteria = dto.ToCriteria(CurrentOrganizationId);
                var journalEntryLines = (await _journalEntryRepository.GetJournalEntryLinesAsync(criteria)).ToList();
                if (dto.ExcludeBeforeOwnerStartingBalance)
                {
                    journalEntryLines = (await _accountingManager.FilterOwnerApAgingJournalEntryLinesAsync(
                        CurrentOrganizationId,
                        journalEntryLines)).ToList();
                }

                var response = journalEntryLines.Select(l => new JournalEntryLineSearchResponseDto(l)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching journal entry lines");
                return ServerError("An error occurred while retrieving journal entry lines");
            }
        }

        [HttpPost("journal-entry-line/reconcile/beginning-balance")]
        public async Task<IActionResult> GetReconcileBeginningBalance([FromBody] GetReconcileBeginningBalanceDto dto)
        {
            if (dto == null)
                return BadRequest("Reconcile beginning balance criteria is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var beginningBalance = await _journalEntryRepository.GetReconcileBeginningBalanceAsync(CurrentOrganizationId, dto.OfficeId, dto.ChartOfAccountId, dto.StatementDate);

                return Ok(new ReconcileBeginningBalanceResponseDto(beginningBalance));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reconcile beginning balance");
                return ServerError("An error occurred while retrieving the reconcile beginning balance");
            }
        }

        [HttpPost("journal-entry-line/reconcile/lines")]
        public async Task<IActionResult> GetReconcileJournalEntryLines([FromBody] GetReconcileJournalEntryLinesDto dto)
        {
            if (dto == null)
                return BadRequest("Reconcile line criteria is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var journalEntryLines = await _journalEntryRepository.GetReconcileJournalEntryLinesAsync(CurrentOrganizationId, dto.OfficeId, dto.ChartOfAccountId, dto.StatementDate);
                var response = journalEntryLines.Select(line => new JournalEntryLineSearchResponseDto(line)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reconcile journal entry lines for account {ChartOfAccountId}", dto.ChartOfAccountId);
                return ServerError("An error occurred while retrieving reconcile journal entry lines");
            }
        }

        [HttpGet("journal-entry/code/{journalEntryCode}")]
        public async Task<IActionResult> GetJournalEntryByCode(string journalEntryCode)
        {
            if (string.IsNullOrWhiteSpace(journalEntryCode))
                return BadRequest("Journal entry code is required");

            try
            {
                var journalEntry = await _journalEntryRepository.GetJournalEntryByCodeAsync(journalEntryCode.Trim(), CurrentOrganizationId);
                if (journalEntry == null)
                    return NotFound("Journal entry not found");

                var response = new JournalEntryResponseDto(journalEntry);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting journal entry by code: {JournalEntryCode}", journalEntryCode);
                return ServerError("An error occurred while retrieving the journal entry");
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
                if (createdJournalEntry == null)
                    return NotFound(new { message = "Accounting is not enabled in this environment." });

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

        [HttpPut("journal-entry-line/reconcile/marks")]
        public async Task<IActionResult> SaveReconcileMarks([FromBody] SaveReconcileMarksDto dto)
        {
            if (dto == null)
                return BadRequest("Reconcile marks data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid reconcile marks data");

            try
            {
                var request = dto.ToModel();
                await _journalEntryRepository.UpdateReconcileMarksAsync(CurrentOrganizationId, request.OfficeId, request.ChartOfAccountId, request.Lines, setClearedOn: false, clearedOn: null, CurrentUser);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving reconcile marks");
                return ServerError("An error occurred while saving reconcile marks");
            }
        }

        [HttpPut("journal-entry-line/reconcile/complete")]
        public async Task<IActionResult> CompleteReconcile([FromBody] CompleteReconcileDto dto)
        {
            if (dto == null)
                return BadRequest("Reconcile completion data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid reconcile completion data");

            try
            {
                var request = dto.ToCompleteModel();
                var clearedOn = DateOnly.FromDateTime(DateTime.Today);

                await _journalEntryRepository.UpdateReconcileMarksAsync(CurrentOrganizationId, request.OfficeId, request.ChartOfAccountId, request.Lines, setClearedOn: true, clearedOn: clearedOn, CurrentUser);

                await _accountingManager.ApplyDocumentPostingStatusFromReconcileAsync(request, CurrentOrganizationId, CurrentUser);

                var updatedAccount = await _accountingRepository.UpdateChartOfAccountReconcileByIdAsync(CurrentOrganizationId, request.OfficeId, request.ChartOfAccountId, request.EndingBalance, request.StatementDate);

                var reconcileDraft = await _accountingRepository.GetReconcileDraftByAccountIdAsync(CurrentOrganizationId, request.OfficeId, request.ChartOfAccountId);
                if (reconcileDraft != null)
                {
                    await _accountingRepository.CreateReconcileAsync(new Reconcile
                    {
                        OrganizationId = reconcileDraft.OrganizationId,
                        OfficeId = reconcileDraft.OfficeId,
                        AccountId = reconcileDraft.AccountId,
                        StatementDate = request.StatementDate,
                        EndingBalance = request.EndingBalance,
                        ServiceChargeAmount = reconcileDraft.ServiceChargeAmount,
                        ServiceChargeDate = reconcileDraft.ServiceChargeDate,
                        ServiceChargeAccountId = reconcileDraft.ServiceChargeAccountId,
                        InterestAmount = reconcileDraft.InterestAmount,
                        InterestDate = reconcileDraft.InterestDate,
                        InterestAccountId = reconcileDraft.InterestAccountId
                    });
                }

                await _accountingRepository.DeleteReconcileDraftByAccountIdAsync(CurrentOrganizationId, request.OfficeId, request.ChartOfAccountId);

                return Ok(new ChartOfAccountResponseDto(updatedAccount));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing reconcile");
                return ServerError("An error occurred while completing the reconciliation");
            }
        }

        [HttpPut("journal-entry")]
        public async Task<IActionResult> UpdateJournalEntry([FromBody] UpdateJournalEntryDto dto)
        {
            if (dto == null)
                return BadRequest("Journal entry data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid journal entry data");

            var periodCheck = await RefuseIfAccountingPeriodClosedAsync(_accountingRepository, CurrentOrganizationId, dto.OfficeId, dto.AccountingPeriod, "update the journal entry");
            if (periodCheck != null)
                return periodCheck;

            try
            {
                var existingJournalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(dto.JournalEntryId, CurrentOrganizationId);
                if (existingJournalEntry == null)
                    return NotFound("Journal entry not found");

                var postingStatusCheck = RefuseIfJournalEntryUpdateNotAllowed(existingJournalEntry.PostingStatusId);
                if (postingStatusCheck != null)
                    return postingStatusCheck;

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
        public async Task<IActionResult> PostJournalEntry(Guid journalEntryId, [FromBody] PostJournalEntryDto? dto)
        {
            if (journalEntryId == Guid.Empty)
                return BadRequest("Journal entry ID is required");

            try
            {
                var journalEntry = await _accountingManager.PostJournalEntryAsync(
                    journalEntryId,
                    CurrentOrganizationId,
                    CurrentUser,
                    dto?.AccountingPeriod);
                var response = new JournalEntryResponseDto(journalEntry);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting journal entry: {JournalEntryId}", journalEntryId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("journal-entry/{journalEntryId}/unpost")]
        public async Task<IActionResult> UnpostJournalEntry(Guid journalEntryId)
        {
            if (journalEntryId == Guid.Empty)
                return BadRequest("Journal entry ID is required");

            try
            {
                var existingJournalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, CurrentOrganizationId);
                if (existingJournalEntry == null)
                    return NotFound("Journal entry not found");

                var postingStatusCheck = RefuseIfJournalEntryUpdateNotAllowed(existingJournalEntry.PostingStatusId);
                if (postingStatusCheck != null)
                    return postingStatusCheck;

                var journalEntry = await _accountingManager.UnpostJournalEntryAsync(journalEntryId, CurrentOrganizationId, CurrentUser);
                var response = new JournalEntryResponseDto(journalEntry);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unposting journal entry: {JournalEntryId}", journalEntryId);
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
                var existingJournalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, CurrentOrganizationId);
                if (existingJournalEntry == null)
                    return NotFound("Journal entry not found");

                var postingStatusCheck = RefuseIfJournalEntryUpdateNotAllowed(existingJournalEntry.PostingStatusId);
                if (postingStatusCheck != null)
                    return postingStatusCheck;

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

        [HttpPut("journal-entry/{journalEntryId}/soft-close")]
        public async Task<IActionResult> SoftCloseJournalEntry(Guid journalEntryId)
        {
            if (journalEntryId == Guid.Empty)
                return BadRequest("Journal entry ID is required");

            try
            {
                var journalEntry = await _accountingManager.SoftCloseJournalEntryAsync(journalEntryId, CurrentOrganizationId, CurrentUser);
                var response = new JournalEntryResponseDto(journalEntry);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft closing journal entry: {JournalEntryId}", journalEntryId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("journal-entry/{journalEntryId}/hard-close")]
        public async Task<IActionResult> HardCloseJournalEntry(Guid journalEntryId)
        {
            if (journalEntryId == Guid.Empty)
                return BadRequest("Journal entry ID is required");

            try
            {
                var journalEntry = await _accountingManager.HardCloseJournalEntryAsync(journalEntryId, CurrentOrganizationId, CurrentUser);
                var response = new JournalEntryResponseDto(journalEntry);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard closing journal entry: {JournalEntryId}", journalEntryId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("journal-entry/close-period")]
        public async Task<IActionResult> CloseAccountingPeriod([FromBody] CloseAccountingPeriodDto dto)
        {
            if (dto == null)
                return BadRequest("Close period data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid close period request");

            try
            {
                var result = await _accountingManager.CloseAccountingPeriodAsync(
                    CurrentOrganizationId,
                    dto.OfficeId,
                    dto.StartDate,
                    dto.EndDate,
                    dto.ToCloseStatus(),
                    dto.JournalEntryIds,
                    CurrentUser);

                return Ok(new CloseAccountingPeriodResultDto(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing accounting period for office {OfficeId}", dto.OfficeId);
                return ServerError("An error occurred while closing the accounting period");
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
                var existingJournalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, CurrentOrganizationId);
                if (existingJournalEntry == null)
                    return NotFound("Journal entry not found");

                var postingStatusCheck = RefuseIfJournalEntryDeleteNotAllowed(existingJournalEntry.PostingStatusId);
                if (postingStatusCheck != null)
                    return postingStatusCheck;

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

using RentAll.Api.Dtos.Accounting.OwnerStatements;
using RentAll.Api.Dtos.Accounting.JournalEntries;

namespace RentAll.Api.Controllers
{
    public partial class AccountingController
    {
        [HttpPost("owner-statement/search")]
        public async Task<IActionResult> SearchOwnerStatements([FromBody] GetOwnerStatementDto dto)
        {
            if (dto == null)
                return BadRequest("Owner statement search criteria is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var criteria = dto.ToCriteria(CurrentOrganizationId);
                var statements = await _accountingManager.GetOwnerStatementsAsync(criteria);
                var response = statements.Select(statement => new OwnerStatementResponseDto(statement)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching owner statements");
                return ServerError("An error occurred while retrieving owner statements");
            }
        }

        [HttpPost("owner-statement/line/search")]
        public async Task<IActionResult> SearchOwnerStatementJournalEntryLines([FromBody] GetOwnerStatementJournalEntryLineDto dto)
        {
            if (dto == null)
                return BadRequest("Owner statement line search criteria is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var criteria = dto.ToCriteria(CurrentOrganizationId);
                var lines = await _accountingManager.GetOwnerStatementJournalEntryLinesAsync(criteria);
                var response = lines.Select(line => new OwnerStatementJournalEntryLineResponseDto(line)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching owner statement journal entry lines");
                return ServerError("An error occurred while retrieving owner statement journal entry lines");
            }
        }

        [HttpPost("owner-statement/property-line/search")]
        public async Task<IActionResult> SearchOwnerStatementPropertyActivityLines([FromBody] GetOwnerStatementPropertyActivityLineDto dto)
        {
            if (dto == null)
                return BadRequest("Owner statement property line search criteria is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var criteria = dto.ToCriteria(CurrentOrganizationId);
                var lines = await _accountingManager.GetOwnerStatementPropertyActivityLinesAsync(criteria);
                var response = lines.Select(line => new OwnerStatementPropertyActivityLineResponseDto(line)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching owner statement property activity lines");
                return ServerError("An error occurred while retrieving owner statement property activity lines");
            }
        }

        [HttpPost("owner-statement/starting-balance")]
        public async Task<IActionResult> CreateOwnerStatementStartingBalance([FromBody] CreateOwnerStatementStartingBalanceDto dto)
        {
            if (dto == null)
                return BadRequest("Owner statement starting balance data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingStartingBalance = await _accountingManager.GetOwnerStatementStartingBalanceAsync(CurrentOrganizationId, dto.OfficeId, dto.OwnerId, dto.PropertyId);
                var hasExistingStartingBalance = existingStartingBalance != null;
                var isChangeRequest = hasExistingStartingBalance && (Math.Abs(existingStartingBalance!.Amount - dto.Amount) > 0.005m || existingStartingBalance.TransactionDate != dto.TransactionDate);
                if (isChangeRequest)
                {
                    if (!IsAdmin() && !IsSuperAdmin())
                        return Unauthorized("Only Admin users can change an existing owner starting balance.");

                    if (!await _authManager.VerifyPasswordAsync(CurrentUser, dto.CurrentPassword))
                        return Unauthorized("Password confirmation failed.");

                    await _accountingManager.DeleteJournalEntryAsync(existingStartingBalance!.JournalEntryId, CurrentOrganizationId);
                }
                else if (hasExistingStartingBalance)
                {
                    var existingJournalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(existingStartingBalance!.JournalEntryId, CurrentOrganizationId);
                    if (existingJournalEntry == null)
                        return NotFound(new { message = "Existing owner starting balance journal entry was not found." });

                    return Ok(new JournalEntryResponseDto(existingJournalEntry));
                }

                var journalEntry = await _accountingManager.CreateOwnerStatementStartingBalanceJournalEntryAsync(CurrentOrganizationId, dto.OfficeId, dto.OwnerId, dto.PropertyId, dto.TransactionDate, dto.Amount, CurrentUser);
                if (journalEntry == null)
                    return NotFound(new { message = "Accounting is not enabled in this environment." });

                var response = new JournalEntryResponseDto(journalEntry);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating owner statement starting balance");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("owner-statement/starting-balance/get")]
        public async Task<IActionResult> GetOwnerStatementStartingBalance([FromBody] GetOwnerStatementStartingBalanceDto dto)
        {
            if (dto == null)
                return BadRequest("Owner statement starting balance criteria is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var entry = await _accountingManager.GetOwnerStatementStartingBalanceAsync(CurrentOrganizationId, dto.OfficeId, dto.OwnerId, dto.PropertyId);
                if (entry == null)
                    return Ok(null);

                return Ok(new OwnerStatementStartingBalanceResponseDto(entry));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving owner statement starting balance");
                return ServerError("An error occurred while retrieving owner statement starting balance");
            }
        }
    }
}

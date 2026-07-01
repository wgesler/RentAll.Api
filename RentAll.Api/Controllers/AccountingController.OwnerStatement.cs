using RentAll.Api.Dtos.Accounting.OwnerStatements;

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
    }
}

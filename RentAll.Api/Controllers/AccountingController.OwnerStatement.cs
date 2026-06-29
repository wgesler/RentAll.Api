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
                var statements = await _accountingRepository.GetOwnerStatementsAsync(criteria);
                var response = statements.Select(statement => new OwnerStatementResponseDto(statement)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching owner statements");
                return ServerError("An error occurred while retrieving owner statements");
            }
        }
    }
}

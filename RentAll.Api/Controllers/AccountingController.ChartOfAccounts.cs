using RentAll.Api.Dtos.Accounting.ChartOfAccounts;

namespace RentAll.Api.Controllers
{
    public partial class AccountingController
    {
        #region Get
        [HttpGet("chart-of-account/office")]
        public async Task<IActionResult> GetChartOfAccountsByOfficeIdsAsync()
        {
            try
            {
                var accounts = await _accountingRepository.GetChartOfAccountsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = accounts.Select(a => new ChartOfAccountResponseDto(a)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart of accounts");
                return ServerError("An error occurred while retrieving chart of accounts");
            }
        }

        [HttpGet("chart-of-account/office/{officeId:int}")]
        public async Task<IActionResult> GetChartOfAccountsByOfficeIdAsync(int officeId)
        {
            try
            {
                if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
                    return Unauthorized("You do not have access to this office's chart of accounts");

                var accounts = await _accountingRepository.GetChartOfAccountsByOfficeIdAsync(CurrentOrganizationId, officeId);
                var response = accounts.Select(a => new ChartOfAccountResponseDto(a)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart of accounts for office {OfficeId}", officeId);
                return ServerError("An error occurred while retrieving chart of accounts");
            }
        }

        [HttpGet("chart-of-account/office/{officeId:int}/accountId/{accountId:int}")]
        public async Task<IActionResult> GetChartOfAccountByIdAsync(int officeId, int accountId)
        {
            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
                return Unauthorized("You do not have access to this office's chart of accounts");

            if (accountId < 0)
                return BadRequest("Invalid account id");

            try
            {
                var account = await _accountingRepository.GetChartOfAccountByIdAsync(CurrentOrganizationId, officeId, accountId);
                if (account == null)
                    return NotFound("Chart of account not found");

                return Ok(new ChartOfAccountResponseDto(account));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart of account {AccountId}", accountId);
                return ServerError("An error occurred while retrieving the chart of account");
            }
        }

        [HttpGet("chart-of-account/office/{officeId:int}/accountNo/{accountNo}")]
        public async Task<IActionResult> GetChartOfAccountByAccountNoAsync(int officeId, string accountNo)
        {
            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
                return Unauthorized("You do not have access to this office's chart of accounts");

            if (string.IsNullOrWhiteSpace(accountNo))
                return BadRequest("Invalid account number");

            try
            {
                var account = await _accountingRepository.GetChartOfAccountByAccountNoAsync(CurrentOrganizationId, officeId, accountNo);
                if (account == null)
                    return NotFound("Chart of account not found");

                return Ok(new ChartOfAccountResponseDto(account));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart of account by account number {AccountNo}", accountNo);
                return ServerError("An error occurred while retrieving the chart of account");
            }
        }
        #endregion

        #region Post
        [HttpPost("chart-of-account")]
        public async Task<IActionResult> CreateChartOfAccount([FromBody] CreateChartOfAccountDto dto)
        {
            if (dto == null)
                return BadRequest("Chart of account data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid chart of account request");

            try
            {
                var chartOfAccount = dto.ToModel();
                chartOfAccount.OrganizationId = CurrentOrganizationId;

                if (await _accountingRepository.ExistsChartOfAccountByAccountNoAsync(chartOfAccount.OrganizationId, chartOfAccount.OfficeId, chartOfAccount.AccountNo))
                    return BadRequest("AccountNo already exists");

                if (chartOfAccount.IsSubaccount && chartOfAccount.SubAccountId.HasValue &&
                    !await _accountingRepository.ExistsChartOfAccountByAccountIdAsync(chartOfAccount.OrganizationId, chartOfAccount.OfficeId, chartOfAccount.SubAccountId.Value))
                {
                    return BadRequest("Parent SubAccountId not found");
                }

                var created = await _accountingRepository.CreateAsync(chartOfAccount);
                return Ok(new ChartOfAccountResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chart of account");
                return ServerError("An error occurred while creating the chart of account");
            }
        }
        #endregion

        #region Put
        [HttpPut("chart-of-account")]
        public async Task<IActionResult> UpdateChartOfAccount([FromBody] UpdateChartOfAccountDto dto)
        {
            if (dto == null)
                return BadRequest("Chart of account data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid chart of account request");

            try
            {
                var existing = await _accountingRepository.GetChartOfAccountByIdAsync(CurrentOrganizationId, dto.OfficeId, dto.AccountId);
                if (existing == null)
                    return NotFound("Chart of account not found");

                var chartOfAccount = dto.ToModel();
                chartOfAccount.OrganizationId = CurrentOrganizationId;

                if (await _accountingRepository.ExistsChartOfAccountByAccountNoAsync(
                    chartOfAccount.OrganizationId, chartOfAccount.OfficeId, chartOfAccount.AccountNo, chartOfAccount.AccountId))
                {
                    return BadRequest("AccountNo already exists");
                }

                if (chartOfAccount.IsSubaccount && chartOfAccount.SubAccountId.HasValue &&
                    !await _accountingRepository.ExistsChartOfAccountByAccountIdAsync(chartOfAccount.OrganizationId, chartOfAccount.OfficeId, chartOfAccount.SubAccountId.Value))
                {
                    return BadRequest("Parent SubAccountId not found");
                }

                var updated = await _accountingRepository.UpdateChartOfAccountByIdAsync(chartOfAccount);
                return Ok(new ChartOfAccountResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chart of account {AccountId}", dto.AccountId);
                return ServerError("An error occurred while updating the chart of account");
            }
        }
        #endregion

        #region Delete
        [HttpDelete("chart-of-account/office/{officeId:int}/accountId/{accountId:int}")]
        public async Task<IActionResult> DeleteChartOfAccountByIdAsync(int officeId, int accountId)
        {
            if (accountId < 0)
                return BadRequest("Invalid account id");

            try
            {
                await _accountingRepository.DeleteChartOfAccountByIdAsync(CurrentOrganizationId, officeId, accountId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chart of account {AccountId}", accountId);
                return ServerError("An error occurred while deleting the chart of account");
            }
        }
        #endregion
    }
}

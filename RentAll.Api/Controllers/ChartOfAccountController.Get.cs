using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.ChartOfAccounts;

namespace RentAll.Api.Controllers
{
	public partial class ChartOfAccountController
	{
		/// <summary>
		/// Get all chart of accounts
		/// </summary>
		/// <returns>List of chart of accounts</returns>
		[HttpGet("office/{officeId:int}")]
		public async Task<IActionResult> GetByOfficeId(int officeId)
		{
			try
			{
				if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
					return Unauthorized("You do not have access to this office's chart of acccounts");

				var chartOfAccounts = await _chartOfAccountRepository.GetAllByOfficeIdAsync(officeId, CurrentOrganizationId);
				var response = chartOfAccounts.Select(c => new ChartOfAccountResponseDto(c)).ToList();
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all chart of accounts");
				return ServerError("An error occurred while retrieving chart of accounts");
			}
		}

		/// <summary>
		/// Get chart of account by ID
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <param name="chartOfAccountId">Chart of Account ID</param>
		/// <returns>Chart of Account</returns>
		[HttpGet("office/{officeId:int}/chartOfAccountId/{chartOfAccountId:int}")]
		public async Task<IActionResult> GetByAccountId(int officeId, int chartOfAccountId)
		{
			if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
				return Unauthorized("You do not have access to this office's chart of acccounts");

			if (chartOfAccountId <= 0)
				return BadRequest("Invalid account");

			try
			{
				var chartOfAccount = await _chartOfAccountRepository.GetByIdAsync(chartOfAccountId, officeId, CurrentOrganizationId);
				if (chartOfAccount == null)
					return NotFound("Chart of Account not found");

				var response = new ChartOfAccountResponseDto(chartOfAccount);
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting chart of account by ID: {chartOfAccountId}", chartOfAccountId);
				return ServerError("An error occurred while retrieving the chart of account");
			}
		}

		/// <summary>
		/// Get chart of account by ID
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <param name="account">Chart of Account</param>
		/// <returns>Chart of Account</returns>
		[HttpGet("office/{officeId:int}/accountNumber/{account:int}")]
		public async Task<IActionResult> GetByAccountNumber(int officeId, int account)
		{
			if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
				return Unauthorized("You do not have access to this office's chart of acccounts");

			if (account <= 0)
				return BadRequest("Invalid account");

			try
			{
				var chartOfAccount = await _chartOfAccountRepository.GetByAccountNumberAsync(account, officeId, CurrentOrganizationId);
				if (chartOfAccount == null)
					return NotFound("Chart of Account not found");

				var response = new ChartOfAccountResponseDto(chartOfAccount);
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting chart of account by ID: {account}", account);
				return ServerError("An error occurred while retrieving the chart of account");
			}
		}
	}
}

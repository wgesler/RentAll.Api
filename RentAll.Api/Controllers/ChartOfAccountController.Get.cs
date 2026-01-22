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
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var chartOfAccounts = await _chartOfAccountRepository.GetAllAsync(CurrentOrganizationId);

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
		/// <param name="chartOfAccountId">Chart of Account ID</param>
		/// <returns>Chart of Account</returns>
		[HttpGet("{chartOfAccountId}")]
		public async Task<IActionResult> GetById(int chartOfAccountId)
		{
			if (chartOfAccountId <= 0)
				return BadRequest("Chart of Account ID is required");

			try
			{
				var chartOfAccount = await _chartOfAccountRepository.GetByIdAsync(chartOfAccountId, CurrentOrganizationId);
				if (chartOfAccount == null)
					return NotFound("Chart of Account not found");

				var response = new ChartOfAccountResponseDto(chartOfAccount);
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting chart of account by ID: {ChartOfAccountId}", chartOfAccountId);
				return ServerError("An error occurred while retrieving the chart of account");
			}
		}
	}
}

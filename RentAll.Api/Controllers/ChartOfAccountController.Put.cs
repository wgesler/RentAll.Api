using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.ChartOfAccounts;

namespace RentAll.Api.Controllers
{
	public partial class ChartOfAccountController
	{
		/// <summary>
		/// Update an existing chart of account
		/// </summary>
		/// <param name="chartOfAccountId">Chart of Account ID</param>
		/// <param name="dto">Chart of Account data</param>
		/// <returns>Updated chart of account</returns>
		[HttpPut("{chartOfAccountId}")]
		public async Task<IActionResult> Update([FromBody] UpdateChartOfAccountDto dto)
		{
			if (dto == null)
				return BadRequest("Chart of Account data is required");

			var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid chart of account request");

			try
			{
				var existingChartOfAccount = await _chartOfAccountRepository.GetByIdAsync(dto.ChartOfAccountId, dto.OfficeId, CurrentOrganizationId);
				if (existingChartOfAccount == null)
					return NotFound("Chart of Account not found");

			if (existingChartOfAccount.AccountId != dto.AccountId)
			{
				if (await _chartOfAccountRepository.ExistsByAccountNumberAsync(dto.AccountId, dto.OfficeId, CurrentOrganizationId))
					return Conflict("Account Number already exists");
			}

				var chartOfAccount = dto.ToModel();
				chartOfAccount.OrganizationId = CurrentOrganizationId;
				var updatedChartOfAccount = await _chartOfAccountRepository.UpdateByIdAsync(chartOfAccount);

				var response = new ChartOfAccountResponseDto(updatedChartOfAccount);
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating chart of account: {ChartOfAccountId}", dto.ChartOfAccountId);
				return ServerError("An error occurred while updating the chart of account");
			}
		}
	}
}

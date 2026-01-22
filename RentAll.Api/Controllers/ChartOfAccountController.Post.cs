using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.ChartOfAccounts;

namespace RentAll.Api.Controllers
{
	public partial class ChartOfAccountController
	{
		/// <summary>
		/// Create a new chart of account
		/// </summary>
		/// <param name="dto">Chart of Account data</param>
		/// <returns>Created chart of account</returns>
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateChartOfAccountDto dto)
		{
			if (dto == null)
				return BadRequest("Chart of Account data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid chart of account data");

			try
			{
				if (await _chartOfAccountRepository.ExistsByAccountNumberAsync(dto.AccountNumber, CurrentOrganizationId))
					return Conflict("Account Number already exists");

				var chartOfAccount = dto.ToModel();
				chartOfAccount.OrganizationId = CurrentOrganizationId;

				var createdChartOfAccount = await _chartOfAccountRepository.CreateAsync(chartOfAccount);

				var response = new ChartOfAccountResponseDto(createdChartOfAccount);
				return CreatedAtAction(nameof(GetById), new { chartOfAccountId = createdChartOfAccount.ChartOfAccountId }, response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating chart of account");
				return ServerError("An error occurred while creating the chart of account");
			}
		}
	}
}

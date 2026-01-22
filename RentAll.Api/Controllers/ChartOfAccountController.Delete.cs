using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public partial class ChartOfAccountController
	{
		/// <summary>
		/// Delete a chart of account
		/// </summary>
		/// <param name="chartOfAccountId">Chart of Account ID</param>
		/// <returns>No content</returns>
		[HttpDelete("{chartOfAccountId}")]
		public async Task<IActionResult> Delete(int chartOfAccountId)
		{
			if (chartOfAccountId <= 0)
				return BadRequest("Chart of Account ID is required");

			try
			{
				var existingChartOfAccount = await _chartOfAccountRepository.GetByIdAsync(chartOfAccountId, CurrentOrganizationId);
				if (existingChartOfAccount == null)
					return NotFound("Chart of Account not found");

				await _chartOfAccountRepository.DeleteByIdAsync(chartOfAccountId, CurrentOrganizationId);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting chart of account: {ChartOfAccountId}", chartOfAccountId);
				return ServerError("An error occurred while deleting the chart of account");
			}
		}
	}
}

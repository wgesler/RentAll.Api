using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public partial class ChartOfAccountController
	{
		/// <summary>
		/// Delete a chart of account
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <param name="chartOfAccountId">Chart of Account ID</param>
		/// <returns>No content</returns>
		[HttpDelete("office/{officeId:int}/account/{chartOfAccountId:guid}")]
		public async Task<IActionResult> Delete(int officeId, Guid chartOfAccountId)
		{
			if (chartOfAccountId == Guid.Empty)
				return BadRequest("Invalid Chart of Account ID");

			try
			{
				await _chartOfAccountRepository.DeleteByIdAsync(chartOfAccountId, officeId, CurrentOrganizationId);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting chart of account: {chartOfAccountId}", chartOfAccountId);
				return ServerError("An error occurred while deleting the chart of account");
			}
		}
	}
}

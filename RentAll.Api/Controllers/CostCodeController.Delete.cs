using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public partial class CostCodeController
	{
		/// <summary>
		/// Delete a cost code
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <param name="costCodeId">Cost Code ID</param>
		/// <returns>No content</returns>
		[HttpDelete("office/{officeId:int}/costcodeid/{costCodeId:int}")]
		public async Task<IActionResult> Delete(int officeId, int costCodeId)
		{
			if (costCodeId <= 0)
				return BadRequest("Invalid Cost Code ID");

			try
			{
				await _costCodeRepository.DeleteByIdAsync(costCodeId, officeId, CurrentOrganizationId);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting cost code: {costCodeId}", costCodeId);
				return ServerError("An error occurred while deleting the cost code");
			}
		}
	}
}

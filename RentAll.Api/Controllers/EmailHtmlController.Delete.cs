using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public partial class EmailHtmlController
	{
		/// <summary>
		/// Delete email html for organization.
		/// </summary>
		[HttpDelete]
		public async Task<IActionResult> Delete()
		{
			try
			{
				var existing = await _emailHtmlRepository.GetByOrganizationIdAsync(CurrentOrganizationId);
				if (existing == null)
					return NotFound("EmailHtml not found");

				await _emailHtmlRepository.DeleteByOrganizationIdAsync(CurrentOrganizationId);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting EmailHtml for organization: {OrganizationId}", CurrentOrganizationId);
				return ServerError("An error occurred while deleting EmailHtml");
			}
		}
	}
}

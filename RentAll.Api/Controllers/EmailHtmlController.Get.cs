using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.EmailHtmls;

namespace RentAll.Api.Controllers
{
	public partial class EmailHtmlController
	{
		/// <summary>
		/// Get email html by organization.
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> GetByOrganization()
		{
			try
			{
				var emailHtml = await _emailHtmlRepository.GetByOrganizationIdAsync(CurrentOrganizationId);
				if (emailHtml == null)
					return NotFound("EmailHtml not found");

				return Ok(new EmailHtmlResponseDto(emailHtml));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting EmailHtml for organization: {OrganizationId}", CurrentOrganizationId);
				return ServerError("An error occurred while retrieving EmailHtml");
			}
		}
	}
}

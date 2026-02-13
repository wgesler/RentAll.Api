using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.EmailHtmls;

namespace RentAll.Api.Controllers
{
	public partial class EmailHtmlController
	{
		/// <summary>
		/// Update email html for organization.
		/// </summary>
		[HttpPut]
		public async Task<IActionResult> Update([FromBody] UpdateEmailHtmlDto dto)
		{
			if (dto == null)
				return BadRequest("EmailHtml data is required");

			var (isValid, errorMessage) = dto.IsValid(CurrentOrganizationId);
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				var existing = await _emailHtmlRepository.GetByOrganizationIdAsync(CurrentOrganizationId);
				if (existing == null)
					return NotFound("EmailHtml not found");

				var emailHtml = dto.ToModel(CurrentUser);
				var updated = await _emailHtmlRepository.UpdateByOrganizationIdAsync(emailHtml);
				return Ok(new EmailHtmlResponseDto(updated));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating EmailHtml for organization: {OrganizationId}", CurrentOrganizationId);
				return ServerError("An error occurred while updating EmailHtml");
			}
		}
	}
}

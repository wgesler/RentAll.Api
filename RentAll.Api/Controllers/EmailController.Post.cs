using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Emails;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
	public partial class EmailController
	{
		/// <summary>
		/// Create a new email.
		/// </summary>
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateEmailDto dto)
		{
			if (dto == null)
				return BadRequest("Email data is required");

			var (isValid, errorMessage) = dto.IsValid(CurrentOrganizationId, CurrentOfficeAccess);
			if (!isValid || !IsValidEmail(dto.ToEmail))
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				var email = dto.ToModel(CurrentUser);
				var result = await _emailManager.SendEmail(email);

				if (result.EmailStatus == EmailStatus.Succeeded)
					return Ok(new EmailResponseDto(result));

				if (result.EmailStatus == EmailStatus.Failed)
					return StatusCode(StatusCodes.Status502BadGateway, new EmailResponseDto(result));

				return Accepted(new EmailResponseDto(result));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating email");
				return ServerError("An error occurred while creating the email");
			}
		}
	}
}

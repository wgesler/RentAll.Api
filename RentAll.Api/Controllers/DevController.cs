using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Dev;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/dev")]
[Authorize]
public class DevController : BaseController
{
	private readonly IWebHostEnvironment _environment;
	private readonly IEmailService _emailService;
	private readonly ILogger<DevController> _logger;

	public DevController(
		IWebHostEnvironment environment,
		IEmailService emailService,
		ILogger<DevController> logger)
	{
		_environment = environment;
		_emailService = emailService;
		_logger = logger;
	}

	/// <summary>
	/// Sends a test email using the configured email provider.
	/// Available only in Development environment.
	/// </summary>
	[HttpPost("email/test")]
	public async Task<IActionResult> SendTestEmail([FromBody] SendTestEmailDto dto, CancellationToken cancellationToken)
	{
		if (!_environment.IsDevelopment())
			return NotFound("This endpoint is available only in Development.");

		if (dto == null)
			return BadRequest("Request body is required.");

		var (isValid, errorMessage) = dto.IsValid();
		if (!isValid)
			return BadRequest(errorMessage ?? "Invalid request data.");

		if (!IsValidEmail(dto.ToEmail))
			return BadRequest("A valid ToEmail address is required.");

		try
		{
			await _emailService.SendEmailAsync(
				new EmailMessage
				{
					ToEmail = dto.ToEmail,
					ToName = dto.ToName,
					Subject = dto.Subject,
					PlainTextContent = dto.PlainTextContent,
					HtmlContent = dto.HtmlContent
				},
				cancellationToken);

			return Ok(new { message = $"Test email sent to {dto.ToEmail}." });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error sending test email to {ToEmail}", dto.ToEmail);
			return ServerError("An error occurred while sending test email.");
		}
	}
}

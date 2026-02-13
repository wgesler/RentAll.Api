using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("api/emailhtml")]
	[Authorize]
	public partial class EmailHtmlController : BaseController
	{
		private readonly IEmailHtmlRepository _emailHtmlRepository;
		private readonly ILogger<EmailHtmlController> _logger;

		public EmailHtmlController(
			IEmailHtmlRepository emailHtmlRepository,
			ILogger<EmailHtmlController> logger)
		{
			_emailHtmlRepository = emailHtmlRepository;
			_logger = logger;
		}
	}
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("api/email")]
	[Authorize]
	public partial class EmailController : BaseController
	{
		private readonly IEmailRepository _emailRepository;
		private readonly IEmailManager _emailManager;
		private readonly ILogger<EmailController> _logger;

		public EmailController(
			IEmailRepository emailRepository,
			IEmailManager emailManager,
			ILogger<EmailController> logger)
		{
			_emailRepository = emailRepository;
			_emailManager = emailManager;
			_logger = logger;
		}
	}
}

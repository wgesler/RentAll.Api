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
        private readonly IEmailRepository _emailRepository;
        private readonly ILogger<EmailHtmlController> _logger;

        public EmailHtmlController(
            IEmailRepository emailRepository,
            ILogger<EmailHtmlController> logger)
        {
            _emailRepository = emailRepository;
            _logger = logger;
        }
    }
}

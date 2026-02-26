using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/email")]
    [Authorize]
    public partial class EmailController : BaseController
    {
        private readonly IEmailRepository _emailRepository;
        private readonly IEmailManager _emailManager;
        private readonly IFileService _fileService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(
            IEmailRepository emailRepository,
            IEmailManager emailManager,
            IFileService fileService,
            ILogger<EmailController> logger)
        {
            _emailRepository = emailRepository;
            _emailManager = emailManager;
            _fileService = fileService;
            _logger = logger;
        }
    }
}

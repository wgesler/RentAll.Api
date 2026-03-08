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
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IEmailRepository _emailRepository;
        private readonly IEmailManager _emailManager;
        private readonly IFileAttachmentHelper _fileAttachmentHelper;
        private readonly IFileService _fileService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(
            IOrganizationRepository organizationRepository,
            IEmailRepository emailRepository,
            IEmailManager emailManager,
            IFileAttachmentHelper fileAttachmentHelper,
            IFileService fileService,
            ILogger<EmailController> logger)
        {
            _organizationRepository = organizationRepository;
            _emailRepository = emailRepository;
            _emailManager = emailManager;
            _fileAttachmentHelper = fileAttachmentHelper;
            _fileService = fileService;
            _logger = logger;
        }
    }
}

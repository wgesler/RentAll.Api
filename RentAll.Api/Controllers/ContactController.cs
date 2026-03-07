using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/contact")]
    [Authorize]
    public partial class ContactController : BaseController
    {
        private readonly IContactManager _contactManager;
        private readonly IContactRepository _contactRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IFileService _fileService;
        private readonly IFileAttachmentHelper _fileAttachmentHelper;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            IContactManager contactManager,
            IContactRepository contactRepository,
            IOrganizationRepository organizationRepository,
            IFileService fileService,
            IFileAttachmentHelper fileAttachmentHelper,
            ILogger<ContactController> logger)
        {
            _contactManager = contactManager;
            _contactRepository = contactRepository;
            _organizationRepository = organizationRepository;
            _fileService = fileService;
            _fileAttachmentHelper = fileAttachmentHelper;
            _logger = logger;
        }
    }
}

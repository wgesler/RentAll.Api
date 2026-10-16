using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Services;
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
        private readonly IPropertyRepository _propertyRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IFileService _fileService;
        private readonly IFileAttachmentHelper _fileAttachmentHelper;
        private readonly IEncryptionService _encryptionService;
        private readonly IExternalApiKeyService _externalApiKeyService;
        private readonly ExternalPropertyOwnerContactResolver _externalPropertyOwnerContactResolver;
        private readonly ExternalPropertyUploadLogService _externalPropertyUploadLogService;
        private readonly IDocumentIntelligenceService _documentIntelligenceService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            IContactManager contactManager,
            IContactRepository contactRepository,
            IPropertyRepository propertyRepository,
            IOrganizationRepository organizationRepository,
            IFileService fileService,
            IFileAttachmentHelper fileAttachmentHelper,
            IEncryptionService encryptionService,
            IExternalApiKeyService externalApiKeyService,
            ExternalPropertyOwnerContactResolver externalPropertyOwnerContactResolver,
            ExternalPropertyUploadLogService externalPropertyUploadLogService,
            IDocumentIntelligenceService documentIntelligenceService,
            ILogger<ContactController> logger)
        {
            _contactManager = contactManager;
            _contactRepository = contactRepository;
            _propertyRepository = propertyRepository;
            _organizationRepository = organizationRepository;
            _fileService = fileService;
            _fileAttachmentHelper = fileAttachmentHelper;
            _encryptionService = encryptionService;
            _externalApiKeyService = externalApiKeyService;
            _externalPropertyOwnerContactResolver = externalPropertyOwnerContactResolver;
            _externalPropertyUploadLogService = externalPropertyUploadLogService;
            _documentIntelligenceService = documentIntelligenceService;
            _logger = logger;
        }
    }
}

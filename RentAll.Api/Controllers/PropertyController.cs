using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/property")]
    [Authorize]
    public partial class PropertyController : BaseController
    {
        private readonly AppSettings _appSettings;
        private readonly IPropertyManager _propertyManager;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationManager _organizationManager;
        private readonly IUserRepository _userRepository;
        private readonly ICalendarManager _calendarManager;
        private readonly IFileAttachmentHelper _fileAttachmentHelper;
        private readonly IFileService _fileService;
        private readonly IExternalApiKeyService _externalApiKeyService;
        private readonly ILogger<PropertyController> _logger;

        public PropertyController(
            IOptions<AppSettings> appSettingsOptions,
            IPropertyManager propertyManager,
            IPropertyRepository propertyRepository,
            IContactRepository contactRepository,
            IOrganizationRepository organizationRepository,
            IOrganizationManager organizationManager,
            IUserRepository userRepository,
            ICalendarManager calendarManager,
            IFileAttachmentHelper fileAttachmentHelper,
            IFileService fileService,
            IExternalApiKeyService externalApiKeyService,
            ILogger<PropertyController> logger)
        {
            _appSettings = appSettingsOptions.Value;
            _propertyManager = propertyManager;
            _propertyRepository = propertyRepository;
            _contactRepository = contactRepository;
            _organizationRepository = organizationRepository;
            _organizationManager = organizationManager;
            _userRepository = userRepository;
            _calendarManager = calendarManager;
            _fileAttachmentHelper = fileAttachmentHelper;
            _fileService = fileService;
            _externalApiKeyService = externalApiKeyService;
            _logger = logger;
        }
    }
}

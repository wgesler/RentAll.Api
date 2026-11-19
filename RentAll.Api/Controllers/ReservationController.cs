using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RentAll.Api.Services;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{

    [ApiController]
    [Route("api/reservation")]
    [Authorize]
    public partial class ReservationController : BaseController
    {
        private readonly IOrganizationManager _organizationManager;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IAccountingManager _accountingManager;
        private readonly IAccountingRepository _accountingRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IContactManager _contactManager;
        private readonly IExternalApiKeyService _externalApiKeyService;
        private readonly ExternalPropertyUploadLogService _externalPropertyUploadLogService;
        private readonly INumberService _numberService;
        private readonly ContactCardStore _contactCardStore;
        private readonly IEmailManager _emailManager;
        private readonly SendGridSettings _sendGridSettings;
        private readonly StorageSettings _storageSettings;
        private readonly ILogger<ReservationController> _logger;

        public ReservationController(
            IOrganizationManager organizationManager,
            IOrganizationRepository organizationRepository,
            IReservationRepository reservationRepository,
            IAccountingManager accountingManager,
            IAccountingRepository accountingRepository,
            IPropertyRepository propertyRepository,
            IContactRepository contactRepository,
            IContactManager contactManager,
            IExternalApiKeyService externalApiKeyService,
            ExternalPropertyUploadLogService externalPropertyUploadLogService,
            INumberService numberService,
            ContactCardStore contactCardStore,
            IEmailManager emailManager,
            IOptions<SendGridSettings> sendGridSettings,
            IOptions<StorageSettings> storageSettings,
            ILogger<ReservationController> logger)
        {
            _organizationManager = organizationManager;
            _organizationRepository = organizationRepository;
            _reservationRepository = reservationRepository;
            _accountingManager = accountingManager;
            _accountingRepository = accountingRepository;
            _propertyRepository = propertyRepository;
            _contactRepository = contactRepository;
            _contactManager = contactManager;
            _externalApiKeyService = externalApiKeyService;
            _externalPropertyUploadLogService = externalPropertyUploadLogService;
            _numberService = numberService;
            _contactCardStore = contactCardStore;
            _emailManager = emailManager;
            _sendGridSettings = sendGridSettings?.Value ?? throw new ArgumentNullException(nameof(sendGridSettings));
            _storageSettings = storageSettings?.Value ?? throw new ArgumentNullException(nameof(storageSettings));
            _logger = logger;
        }
    }
}

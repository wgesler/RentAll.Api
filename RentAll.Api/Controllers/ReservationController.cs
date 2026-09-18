using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Services;
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
        private readonly IPropertyRepository _propertyRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IContactManager _contactManager;
        private readonly IExternalApiKeyService _externalApiKeyService;
        private readonly ExternalPropertyUploadLogService _externalPropertyUploadLogService;
        private readonly ILogger<ReservationController> _logger;

        public ReservationController(
            IOrganizationManager organizationManager,
            IOrganizationRepository organizationRepository,
            IReservationRepository reservationRepository,
            IAccountingManager accountingManager,
            IPropertyRepository propertyRepository,
            IContactRepository contactRepository,
            IContactManager contactManager,
            IExternalApiKeyService externalApiKeyService,
            ExternalPropertyUploadLogService externalPropertyUploadLogService,
            ILogger<ReservationController> logger)
        {
            _organizationManager = organizationManager;
            _organizationRepository = organizationRepository;
            _reservationRepository = reservationRepository;
            _accountingManager = accountingManager;
            _propertyRepository = propertyRepository;
            _contactRepository = contactRepository;
            _contactManager = contactManager;
            _externalApiKeyService = externalApiKeyService;
            _externalPropertyUploadLogService = externalPropertyUploadLogService;
            _logger = logger;
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{

    [ApiController]
    [Route("api/reservation")]
    [Authorize]
    public partial class ReservationController : BaseController
    {
        private readonly IOrganizationManager _organizationManager;
        private readonly IReservationRepository _reservationRepository;
        private readonly IAccountingRepository _accountingRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IContactRepository _contactRepository;
        private readonly ILogger<ReservationController> _logger;

        public ReservationController(
            IOrganizationManager organizationManager,
            IReservationRepository reservationRepository,
            IAccountingRepository accountingRepository,
            IPropertyRepository propertyRepository,
            IContactRepository contactRepository,
            ILogger<ReservationController> logger)
        {
            _organizationManager = organizationManager;
            _reservationRepository = reservationRepository;
            _accountingRepository = accountingRepository;
            _propertyRepository = propertyRepository;
            _contactRepository = contactRepository;
            _logger = logger;
        }
    }
}

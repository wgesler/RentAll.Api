using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/accounting")]
    [Authorize]
    public partial class AccountingController : BaseController
    {
        private readonly IAccountingRepository _accountingRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IAccountingManager _accountingManager;
        private readonly ILogger<AccountingController> _logger;

        public AccountingController(
            IAccountingRepository accountingRepository,
            IReservationRepository reservationRepository,
            IOrganizationRepository organizationRepository,
            IAccountingManager accountingManager,
            ILogger<AccountingController> logger)
        {
            _accountingRepository = accountingRepository;
            _reservationRepository = reservationRepository;
            _organizationRepository = organizationRepository;
            _accountingManager = accountingManager;
            _logger = logger;
        }
    }
}

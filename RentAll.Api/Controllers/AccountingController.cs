using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/accounting")]
    [Authorize]
    public partial class AccountingController : BaseController
    {
        private readonly IAccountingRepository _accountingRepository;
        private readonly IJournalEntryRepository _journalEntryRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IAccountingManager _accountingManager;
        private readonly IFeatureFlagService _featureFlagService;
        private readonly ILogger<AccountingController> _logger;

        public AccountingController(
            IAccountingRepository accountingRepository,
            IJournalEntryRepository journalEntryRepository,
            IReservationRepository reservationRepository,
            IOrganizationRepository organizationRepository,
            IAccountingManager accountingManager,
            IFeatureFlagService featureFlagService,
            ILogger<AccountingController> logger)
        {
            _accountingRepository = accountingRepository;
            _journalEntryRepository = journalEntryRepository;
            _reservationRepository = reservationRepository;
            _organizationRepository = organizationRepository;
            _accountingManager = accountingManager;
            _featureFlagService = featureFlagService;
            _logger = logger;
        }
    }
}

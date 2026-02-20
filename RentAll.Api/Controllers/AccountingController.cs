using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IAccountingManager _accountingManager;
        private readonly ILogger<AccountingController> _logger;

        public AccountingController(
            IAccountingRepository accountingRepository,
            IReservationRepository reservationRepository,
            IAccountingManager accountingManager,
            ILogger<AccountingController> logger)
        {
            _accountingRepository = accountingRepository;
            _reservationRepository = reservationRepository;
            _accountingManager = accountingManager;
            _logger = logger;
        }
    }
}

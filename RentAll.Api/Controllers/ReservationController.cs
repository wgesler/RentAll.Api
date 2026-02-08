using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
		private readonly IAccountingManager _accountingManager;
		private readonly ILogger<ReservationController> _logger;

		public ReservationController(
			IOrganizationManager organizationManager,
			IReservationRepository reservationRepository,
			IAccountingManager accountingManager,
			ILogger<ReservationController> logger)
		{
			_organizationManager = organizationManager;
			_reservationRepository = reservationRepository;
			_accountingManager = accountingManager;
			_logger = logger;
		}
	}
}


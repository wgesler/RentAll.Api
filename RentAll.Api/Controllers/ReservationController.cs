using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{

    [ApiController]
    [Route("reservation")]
    [Authorize]
    public partial class ReservationController : BaseController
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ILogger<ReservationController> _logger;

        public ReservationController(
            IReservationRepository reservationRepository,
            ILogger<ReservationController> logger)
        {
            _reservationRepository = reservationRepository;
            _logger = logger;
        }
    }
}


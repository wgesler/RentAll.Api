using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("rental")]
    [Authorize]
    public partial class RentalController : BaseController
    {
        private readonly IRentalRepository _rentalRepository;
        private readonly ILogger<RentalController> _logger;

        public RentalController(
            IRentalRepository rentalRepository,
            ILogger<RentalController> logger)
        {
            _rentalRepository = rentalRepository;
            _logger = logger;
        }
    }
}

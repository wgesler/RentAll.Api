using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/leaseinformation")]
    [Authorize]
    public partial class LeaseInformationController : BaseController
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IContactRepository _contactRepository;
        private readonly ILogger<LeaseInformationController> _logger;

        public LeaseInformationController(
            IReservationRepository reservationRepository,
            IPropertyRepository propertyRepository,
            IContactRepository contactRepository,
            ILogger<LeaseInformationController> logger)
        {
            _reservationRepository = reservationRepository;
            _propertyRepository = propertyRepository;
            _contactRepository = contactRepository;
            _logger = logger;
        }
    }
}


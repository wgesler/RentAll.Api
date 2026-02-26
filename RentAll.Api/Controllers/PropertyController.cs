using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/property")]
    [Authorize]
    public partial class PropertyController : BaseController
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICalendarManager _calendarManager;
        private readonly ILogger<PropertyController> _logger;

        public PropertyController(
            IPropertyRepository propertyRepository,
            IContactRepository contactRepository,
            IUserRepository userRepository,
            ICalendarManager calendarManager,
            ILogger<PropertyController> logger)
        {
            _propertyRepository = propertyRepository;
            _contactRepository = contactRepository;
            _userRepository = userRepository;
            _calendarManager = calendarManager;
            _logger = logger;
        }
    }
}

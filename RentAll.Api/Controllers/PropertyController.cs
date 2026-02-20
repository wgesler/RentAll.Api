using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ICalendarManager _calendarManager;
        private readonly ILogger<PropertyController> _logger;

        public PropertyController(
            IPropertyRepository propertyRepository,
            ICalendarManager calendarManager,
            ILogger<PropertyController> logger)
        {
            _propertyRepository = propertyRepository;
            _calendarManager = calendarManager;
            _logger = logger;
        }
    }
}

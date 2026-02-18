using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("api/property")]
	[Authorize]
	public partial class PropertyController : BaseController
	{
		private readonly IPropertyRepository _propertyRepository;
		private readonly IPropertySelectionRepository _propertySelectionRepository;
		private readonly ICalendarService _calendarService;
		private readonly ILogger<PropertyController> _logger;

		public PropertyController(
			IPropertyRepository propertyRepository,
			IPropertySelectionRepository propertySelectionRepository,
			ICalendarService calendarService,
			ILogger<PropertyController> logger)
		{
			_propertyRepository = propertyRepository;
			_propertySelectionRepository = propertySelectionRepository;
			_calendarService = calendarService;
			_logger = logger;
		}
	}
}
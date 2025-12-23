using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("propertywelcome")]
	[Authorize]
	public partial class PropertyWelcomeController : BaseController
	{
		private readonly IPropertyWelcomeRepository _propertyWelcomeRepository;
		private readonly IPropertyRepository _propertyRepository;
		private readonly ILogger<PropertyWelcomeController> _logger;

		public PropertyWelcomeController(
			IPropertyWelcomeRepository propertyWelcomeRepository,
			IPropertyRepository propertyRepository,
			ILogger<PropertyWelcomeController> logger)
		{
			_propertyWelcomeRepository = propertyWelcomeRepository;
			_propertyRepository = propertyRepository;
			_logger = logger;
		}
	}
}



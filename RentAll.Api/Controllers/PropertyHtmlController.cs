using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("api/propertyhtml")]
	[Authorize]
	public partial class PropertyHtmlController : BaseController
	{
		private readonly IPropertyHtmlRepository _propertyHtmlRepository;
		private readonly IPropertyRepository _propertyRepository;
		private readonly ILogger<PropertyHtmlController> _logger;

		public PropertyHtmlController(
			IPropertyHtmlRepository propertyHtmlRepository,
			IPropertyRepository propertyRepository,
			ILogger<PropertyHtmlController> logger)
		{
			_propertyHtmlRepository = propertyHtmlRepository;
			_propertyRepository = propertyRepository;
			_logger = logger;
		}
	}
}



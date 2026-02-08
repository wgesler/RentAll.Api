using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("api/propertyletter")]
	[Authorize]
	public partial class PropertyLetterController : BaseController
	{
		private readonly IPropertyLetterRepository _propertyLetterRepository;
		private readonly IPropertyRepository _propertyRepository;
		private readonly ILogger<PropertyLetterController> _logger;

		public PropertyLetterController(
			IPropertyLetterRepository propertyLetterRepository,
			IPropertyRepository propertyRepository,
			ILogger<PropertyLetterController> logger)
		{
			_propertyLetterRepository = propertyLetterRepository;
			_propertyRepository = propertyRepository;
			_logger = logger;
		}
	}
}



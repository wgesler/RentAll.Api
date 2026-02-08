using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("api/area")]
	[Authorize]
	public partial class AreaController : BaseController
	{
		private readonly IOrganizationManager _organizationManager;
		private readonly IAreaRepository _areaRepository;
		private readonly ILogger<AreaController> _logger;

		public AreaController(
			IOrganizationManager organizationManager,
			IAreaRepository areaRepository,
			ILogger<AreaController> logger)
		{
			_organizationManager = organizationManager;
			_areaRepository = areaRepository;
			_logger = logger;
		}
	}
}






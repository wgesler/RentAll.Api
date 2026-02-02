using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("region")]
	[Authorize]
	public partial class RegionController : BaseController
	{
		private readonly IOrganizationManager _organizationManager;
		private readonly IRegionRepository _regionRepository;
		private readonly ILogger<RegionController> _logger;

		public RegionController(
			IOrganizationManager organizationManager,
			IRegionRepository regionRepository,
			ILogger<RegionController> logger)
		{
			_organizationManager = organizationManager;
			_regionRepository = regionRepository;
			_logger = logger;
		}
	}
}






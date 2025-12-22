using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("building")]
    [Authorize]
    public partial class BuildingController : BaseController
    {
		private readonly IOrganizationManager _organizationManager;
		private readonly IBuildingRepository _buildingRepository;
        private readonly ILogger<BuildingController> _logger;

        public BuildingController(
			IOrganizationManager organizationManager,
			IBuildingRepository buildingRepository,
            ILogger<BuildingController> logger)
        {
			_organizationManager = organizationManager;
			_buildingRepository = buildingRepository;
            _logger = logger;
        }
    }
}





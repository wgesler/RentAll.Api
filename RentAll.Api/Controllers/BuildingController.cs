using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/building")]
    [Authorize]
    public partial class BuildingController : BaseController
    {
        private readonly IOrganizationManager _organizationManager;
        private readonly IOrganizationRepository _officeRepository;
        private readonly ILogger<BuildingController> _logger;

        public BuildingController(
            IOrganizationManager organizationManager,
            IOrganizationRepository officeRepository,
            ILogger<BuildingController> logger)
        {
            _organizationManager = organizationManager;
            _officeRepository = officeRepository;
            _logger = logger;
        }
    }
}






using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/region")]
    [Authorize]
    public partial class RegionController : BaseController
    {
        private readonly IOrganizationManager _organizationManager;
        private readonly IOrganizationRepository _officeRepository;
        private readonly ILogger<RegionController> _logger;

        public RegionController(
            IOrganizationManager organizationManager,
            IOrganizationRepository officeRepository,
            ILogger<RegionController> logger)
        {
            _organizationManager = organizationManager;
            _officeRepository = officeRepository;
            _logger = logger;
        }
    }
}






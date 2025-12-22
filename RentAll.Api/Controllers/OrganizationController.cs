using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("organization")]
    [Authorize]
    public partial class OrganizationController : BaseController
    {
        private readonly IOrganizationManager _organizationManager;
		private readonly IOrganizationRepository _organizationRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<OrganizationController> _logger;

        public OrganizationController(
			IOrganizationManager organizationManager,
			IOrganizationRepository organizationRepository,
            IUserRepository userRepository,
            ILogger<OrganizationController> logger)
        {
			_organizationManager = organizationManager;
			_organizationRepository = organizationRepository;
            _userRepository = userRepository;
            _logger = logger;
        }
    }
}





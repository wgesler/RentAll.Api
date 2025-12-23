using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

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
        private readonly IFileService _fileService;
        private readonly ILogger<OrganizationController> _logger;

        public OrganizationController(
			IOrganizationManager organizationManager,
			IOrganizationRepository organizationRepository,
            IUserRepository userRepository,
            IFileService fileService,
            ILogger<OrganizationController> logger)
        {
			_organizationManager = organizationManager;
			_organizationRepository = organizationRepository;
            _userRepository = userRepository;
            _fileService = fileService;
            _logger = logger;
        }
    }
}





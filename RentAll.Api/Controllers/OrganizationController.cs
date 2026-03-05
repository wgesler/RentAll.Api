using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/organization")]
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

        private string? GetOfficeName(int? officeId)
        {
            if (!officeId.HasValue)
                return null;
            var office = _organizationRepository.GetOfficeByIdAsync(officeId.Value, CurrentOrganizationId).GetAwaiter().GetResult();
            return office?.Name;
        }
    }
}





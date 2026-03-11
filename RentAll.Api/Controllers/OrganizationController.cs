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
        private readonly IAccountingManager _accountingManager;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFileService _fileService;
        private readonly IFileAttachmentHelper _fileAttachmentHelper;
        private readonly ILogger<OrganizationController> _logger;

        public OrganizationController(
            IOrganizationManager organizationManager,
            IAccountingManager accountingManager,
            IOrganizationRepository organizationRepository,
            IUserRepository userRepository,
            IFileService fileService,
            IFileAttachmentHelper fileAttachmentHelper,
            ILogger<OrganizationController> logger)
        {
            _organizationManager = organizationManager;
            _accountingManager = accountingManager;
            _organizationRepository = organizationRepository;
            _userRepository = userRepository;
            _fileService = fileService;
            _fileAttachmentHelper = fileAttachmentHelper;
            _logger = logger;
        }

        private async Task<string?> GetOfficeNameAsync(int? officeId)
        {
            if (!officeId.HasValue)
                return null;
            var office = await _organizationRepository.GetOfficeByIdAsync(officeId.Value, CurrentOrganizationId);
            return office?.Name;
        }
    }
}





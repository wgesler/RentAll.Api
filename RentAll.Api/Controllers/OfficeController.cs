using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/office")]
    [Authorize]
    public partial class OfficeController : BaseController
    {
        private readonly IOrganizationManager _organizationManager;
        private readonly IOrganizationRepository _officeRepository;
        private readonly IFileService _fileService;
        private readonly ILogger<OfficeController> _logger;

        public OfficeController(
            IOrganizationManager organizationManager,
            IOrganizationRepository officeRepository,
            IFileService fileService,
            ILogger<OfficeController> logger)
        {
            _organizationManager = organizationManager;
            _officeRepository = officeRepository;
            _fileService = fileService;
            _logger = logger;
        }
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("office")]
    [Authorize]
    public partial class OfficeController : BaseController
    {
		private readonly IOrganizationManager _organizationManager;
		private readonly IOfficeRepository _officeRepository;
		private readonly IOfficeConfigurationRepository _officeConfigurationRepository;
		private readonly IFileService _fileService;
		private readonly ILogger<OfficeController> _logger;

        public OfficeController(
			IOrganizationManager organizationManager,
		    IOfficeRepository officeRepository,
			IOfficeConfigurationRepository officeConfigurationRepository,
			IFileService fileService,
			ILogger<OfficeController> logger)
        {
			_organizationManager = organizationManager;
			_officeRepository = officeRepository;
			_officeConfigurationRepository = officeConfigurationRepository;
			_fileService = fileService;
            _logger = logger;
        }
    }
}


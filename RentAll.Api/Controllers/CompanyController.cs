using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/company")]
    [Authorize]
    public partial class CompanyController : BaseController
    {
        private readonly IOrganizationManager _organizationManager;
        private readonly ICompaniesRepository _companiesRepository;
        private readonly IFileService _fileService;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(
            IOrganizationManager organizationManager,
            ICompaniesRepository companiesRepository,
            IFileService fileService,
            ILogger<CompanyController> logger)
        {
            _organizationManager = organizationManager;
            _companiesRepository = companiesRepository;
            _fileService = fileService;
            _logger = logger;
        }
    }
}

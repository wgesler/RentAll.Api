using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("company")]
    [Authorize]
    public partial class CompanyController : BaseController
    {
        private readonly IOrganizationManager _organizationManager;
        private readonly ICompanyRepository _companyRepository;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(
			IOrganizationManager organizationManager,
		    ICompanyRepository companyRepository,
            ILogger<CompanyController> logger)
        {
			_organizationManager = organizationManager;
			_companyRepository = companyRepository;
            _logger = logger;
        }
    }
}

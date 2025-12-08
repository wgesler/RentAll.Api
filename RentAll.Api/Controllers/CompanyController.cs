using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("company")]
    [Authorize]
    public partial class CompanyController : BaseController
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(
            ICompanyRepository companyRepository,
            ILogger<CompanyController> logger)
        {
            _companyRepository = companyRepository;
            _logger = logger;
        }
    }
}

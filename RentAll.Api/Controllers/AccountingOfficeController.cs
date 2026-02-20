using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/accounting-office")]
    [Authorize]
    public partial class AccountingOfficeController : BaseController
    {
        private readonly IOrganizationRepository _officeRepository;
        private readonly IFileService _fileService;
        private readonly ILogger<AccountingOfficeController> _logger;

        public AccountingOfficeController(
            IOrganizationRepository officeRepository,
            IFileService fileService,
            ILogger<AccountingOfficeController> logger)
        {
            _officeRepository = officeRepository;
            _fileService = fileService;
            _logger = logger;
        }
    }
}

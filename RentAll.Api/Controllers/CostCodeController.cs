using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/costcode")]
    [Authorize]
    public partial class CostCodeController : BaseController
    {
        private readonly IAccountingRepository _accountingRepository;
        private readonly ILogger<CostCodeController> _logger;

        public CostCodeController(
            IAccountingRepository accountingRepository,
            ILogger<CostCodeController> logger)
        {
            _accountingRepository = accountingRepository;
            _logger = logger;
        }
    }
}

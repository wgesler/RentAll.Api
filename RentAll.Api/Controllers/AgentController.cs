using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/agent")]
    [Authorize]
    public partial class AgentController : BaseController
    {
        private readonly IOrganizationRepository _officeRepository;
        private readonly ILogger<AgentController> _logger;

        public AgentController(
            IOrganizationRepository officeRepository,
            ILogger<AgentController> logger)
        {
            _officeRepository = officeRepository;
            _logger = logger;
        }
    }
}







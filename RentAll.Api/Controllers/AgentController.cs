using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("agent")]
    [Authorize]
    public partial class AgentController : BaseController
    {
        private readonly IAgentRepository _agentRepository;
        private readonly ILogger<AgentController> _logger;

        public AgentController(
            IAgentRepository agentRepository,
            ILogger<AgentController> logger)
        {
            _agentRepository = agentRepository;
            _logger = logger;
        }
    }
}


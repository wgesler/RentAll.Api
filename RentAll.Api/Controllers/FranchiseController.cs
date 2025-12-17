using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("franchise")]
    [Authorize]
    public partial class FranchiseController : BaseController
    {
		private readonly IOrganizationManager _organizationManager;
		private readonly IFranchiseRepository _franchiseRepository;
        private readonly ILogger<FranchiseController> _logger;

        public FranchiseController(
			IOrganizationManager organizationManager,
		    IFranchiseRepository franchiseRepository,
            ILogger<FranchiseController> logger)
        {
			_organizationManager = organizationManager;
			_franchiseRepository = franchiseRepository;
            _logger = logger;
        }
    }
}



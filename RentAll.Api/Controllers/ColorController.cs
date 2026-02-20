using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/color")]
    [Authorize]
    public partial class ColorController : BaseController
    {
        private readonly IOrganizationRepository _officeRepository;
        private readonly ILogger<ColorController> _logger;

        public ColorController(
            IOrganizationRepository officeRepository,
            ILogger<ColorController> logger)
        {
            _officeRepository = officeRepository;
            _logger = logger;
        }
    }
}


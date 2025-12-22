using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("vendor")]
    [Authorize]
    public partial class VendorController : BaseController
    {
        private readonly IOrganizationManager _organizationManager;
        private readonly IVendorRepository _vendorRepository;
        private readonly ILogger<VendorController> _logger;

        public VendorController(
			IOrganizationManager organizationManager,
		    IVendorRepository vendorRepository,
            ILogger<VendorController> logger)
        {
			_organizationManager = organizationManager;
			_vendorRepository = vendorRepository;
            _logger = logger;
        }
    }
}




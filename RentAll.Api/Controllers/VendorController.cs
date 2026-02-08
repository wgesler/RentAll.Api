using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("api/vendor")]
	[Authorize]
	public partial class VendorController : BaseController
	{
		private readonly IOrganizationManager _organizationManager;
		private readonly IVendorRepository _vendorRepository;
		private readonly IFileService _fileService;
		private readonly ILogger<VendorController> _logger;

		public VendorController(
			IOrganizationManager organizationManager,
			IVendorRepository vendorRepository,
			IFileService fileService,
			ILogger<VendorController> logger)
		{
			_organizationManager = organizationManager;
			_vendorRepository = vendorRepository;
			_fileService = fileService;
			_logger = logger;
		}
	}
}




using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("leaseinformation")]
	[Authorize]
	public partial class LeaseInformationController : BaseController
	{
		private readonly ILeaseInformationRepository _leaseInformationRepository;
		private readonly IPropertyRepository _propertyRepository;
		private readonly IContactRepository _contactRepository;
		private readonly ILogger<LeaseInformationController> _logger;

		public LeaseInformationController(
			ILeaseInformationRepository leaseInformationRepository,
			IPropertyRepository propertyRepository,
			IContactRepository contactRepository,
			ILogger<LeaseInformationController> logger)
		{
			_leaseInformationRepository = leaseInformationRepository;
			_propertyRepository = propertyRepository;
			_contactRepository = contactRepository;
			_logger = logger;
		}
	}
}


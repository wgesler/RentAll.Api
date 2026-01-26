using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("costcode")]
	[Authorize]
	public partial class CostCodeController : BaseController
	{
		private readonly ICostCodeRepository _costCodeRepository;
		private readonly ILogger<CostCodeController> _logger;

		public CostCodeController(
			ICostCodeRepository costCodeRepository,
			ILogger<CostCodeController> logger)
		{
			_costCodeRepository = costCodeRepository;
			_logger = logger;
		}
	}
}

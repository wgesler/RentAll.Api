using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("chartofaccount")]
	[Authorize]
	public partial class ChartOfAccountController : BaseController
	{
		private readonly IChartOfAccountRepository _chartOfAccountRepository;
		private readonly ILogger<ChartOfAccountController> _logger;

		public ChartOfAccountController(
			IChartOfAccountRepository chartOfAccountRepository,
			ILogger<ChartOfAccountController> logger)
		{
			_chartOfAccountRepository = chartOfAccountRepository;
			_logger = logger;
		}
	}
}

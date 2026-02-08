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
		private readonly IColorRepository _colorRepository;
		private readonly ILogger<ColorController> _logger;

		public ColorController(
			IColorRepository colorRepository,
			ILogger<ColorController> logger)
		{
			_colorRepository = colorRepository;
			_logger = logger;
		}
	}
}


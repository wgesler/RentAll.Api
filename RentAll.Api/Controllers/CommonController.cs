using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("api/common")]
	[AllowAnonymous]
	public partial class CommonController : BaseController
	{
		private readonly AppSettings _appSettings;
		private readonly IDailyQuoteService _dailyQuoteService;
		private readonly ICalendarService _calendarService;
		private readonly ICommonRepository _commonRepository;
		private readonly IReservationRepository _reservationRepository;
		private readonly ILogger<CommonController> _logger;

		public CommonController(
			IOptions<AppSettings> options,
			IDailyQuoteService dailyQuoteService,
			ICommonRepository commonRepository,
			ICalendarService calendarService,
			IReservationRepository reservationRepository,
			ILogger<CommonController> logger)
		{
			_appSettings = options.Value;
			_dailyQuoteService = dailyQuoteService;
			_commonRepository = commonRepository;
			_calendarService = calendarService;
			_reservationRepository = reservationRepository;
			_logger = logger;
		}
	}
}

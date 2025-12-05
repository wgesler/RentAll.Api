using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RentAll.Api.Configuration;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
	public partial class CommonController : ControllerBase
	{
		private readonly AppSettings _appSettings;
		private readonly IDailyQuoteService _dailyQuoteService;

		public CommonController(
			IOptions<AppSettings> options,
			IDailyQuoteService dailyQuoteService)
		{
			_appSettings = options.Value;
			_dailyQuoteService = dailyQuoteService;
		}
	}
}

using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Common;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Controllers
{
	public partial class CommonController
	{
		/// <summary>
		/// Get daily quote
		/// </summary>        
		/// <returns>Daily Quote</returns>
		[HttpGet("daily-quote")]
		public async Task<DailyQuoteResponse> GetDailyQuote()
		{
			DailyQuote quote;
			if (_appSettings.Environment == "Development" || _appSettings.Environment == "Local")
				quote = await _dailyQuoteService.GetDailyDadJoke();
			else
				quote = await _dailyQuoteService.GetDailyQuote();
			return new DailyQuoteResponse(quote);
		}

	}
}

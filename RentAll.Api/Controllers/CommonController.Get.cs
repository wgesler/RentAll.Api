using System;
using System.Linq;
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
		public async Task<IActionResult> GetDailyQuote()
		{
			try
			{
				DailyQuote quote;
				if (_appSettings.Environment == "Development" || _appSettings.Environment == "Local")
					quote = await _dailyQuoteService.GetDailyDadJoke();
				else
					quote = await _dailyQuoteService.GetDailyQuote();
				return Ok(new DailyQuoteResponse(quote));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting daily quote.");
				return ServerError("An error occurred while retrieving daily quote");
			}
		}

		/// <summary>
		/// Get all states
		/// </summary>        
		/// <returns>List of states with two-digit code and name</returns>
		[HttpGet("state")]
		public async Task<IActionResult> GetStates()
		{
			try
			{
				var states = await _commonRepository.GetAllStatesAsync();
				var response = states.Select(s => new StateResponseDto(s));
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting states");
				return ServerError("An error occurred while retrieving states");
			}
		}
	}
}

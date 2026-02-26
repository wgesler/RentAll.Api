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
    }
}

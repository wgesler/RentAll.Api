using Newtonsoft.Json;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Common;

namespace RentAll.Infrastructure.Services;

public class DailyQuoteService : IDailyQuoteService
{
	private static DailyQuote _defaultQuote = new DailyQuote { a = "Ayn Rand", q = "Wealth is the product of man's capacity to think.", h = "" };
	private static DailyQuote _dailyQuote = _defaultQuote;
	private static DateOnly _dailyQuoteDate = new DateOnly();
	private readonly HttpClient _httpClient;

	public DailyQuoteService()
    {
		_httpClient = new HttpClient();
		_httpClient.DefaultRequestHeaders.Add("Get", "application/json");
	}

	public async Task<DailyQuote> GetDailyQuote()
	{
		DateOnly currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

		// Only hit this api once a day as it's the free version
		if (_dailyQuoteDate == currentDate)
			return _dailyQuote;

		try
		{
			var response = await _httpClient.GetAsync("https://zenquotes.io/api/random");
			string content = await response.Content.ReadAsStringAsync();
			var quote = JsonConvert.DeserializeObject<List<DailyQuote>>(content);

			_dailyQuote = quote == null ? _defaultQuote : quote.FirstOrDefault()!;
			_dailyQuoteDate = currentDate;

			return _dailyQuote;
		}
		catch(Exception ex)
		{
			Console.WriteLine(ex);
		}

		return _defaultQuote;			   
	}

	public async Task<DailyQuote> GetDailyDadJoke()
	{
        // Dad joke source: https://icanhazdadjoke.com/api
		// No API key required
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "ReProTool Dev (https://www.4tier.com/)");

		DateOnly currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

		// Only hit this api once a day as it's the free version
		if (_dailyQuoteDate == currentDate)
			return _dailyQuote;

		try
		{
			var response = await _httpClient.GetAsync("https://icanhazdadjoke.com/");
			string content = await response.Content.ReadAsStringAsync();
			var quote = JsonConvert.DeserializeObject<DadJoke>(content);

			_dailyQuote = quote == null ? _defaultQuote : quote.GetAsDailyQuote();
			_dailyQuoteDate = currentDate;

			return _dailyQuote;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
		}

		return _defaultQuote;
	}
}

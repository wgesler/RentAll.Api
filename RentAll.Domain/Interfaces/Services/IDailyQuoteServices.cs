using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Services;

public interface IDailyQuoteService
{
    Task<DailyQuote> GetDailyQuote();
    Task<DailyQuote> GetDailyDadJoke();
}


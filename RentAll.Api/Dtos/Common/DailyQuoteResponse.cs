using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Common;

public class DailyQuoteResponse
{
    public string Quote { get; set; }
    public string Author { get; set; }

    public DailyQuoteResponse(DailyQuote quote)
    {
        Quote = quote.q;
        Author = quote.a;
    }
}

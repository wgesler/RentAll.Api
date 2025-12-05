namespace RentAll.Domain.Models.Common;

public class DadJoke
{
	public string id { get; set; } = string.Empty;
	public string joke { get; set; } = string.Empty;
	public int status { get; set; }


	public DailyQuote GetAsDailyQuote()
    {
        var quote = new DailyQuote() { q = joke };
        return quote;
    }
}


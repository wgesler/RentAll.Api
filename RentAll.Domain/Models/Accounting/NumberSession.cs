namespace RentAll.Domain.Models.Accounting;

public class NumberSession
{
    public string SessKey { get; set; } = string.Empty;
    public int MerchId { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
}

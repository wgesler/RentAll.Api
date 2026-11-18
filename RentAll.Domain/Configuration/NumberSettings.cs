namespace RentAll.Domain.Configuration;

public class NumberSettings
{
    public string? KeyVaultUri { get; set; }
    public string AuthenticateUrl { get; set; } = string.Empty;
    public string InitFormUrl { get; set; } = string.Empty;
    public string TransactionDetailUrl { get; set; } = string.Empty;
    public string? PostUrl { get; set; }
    public string? RedirectUrl { get; set; }
    public string? EIndex { get; set; }
}

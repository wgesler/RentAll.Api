namespace RentAll.Domain.Configuration;

public class NumberSettings
{
    public string? KeyVaultUri { get; set; }
    public string? PostUrl { get; set; }
    public string? RedirectUrl { get; set; }

    /// <summary>
    /// Base address of the RentAll web app (e.g. https://www.rentallexchange.com). Relative payment page URLs
    /// (Accounting.PaymentPage rows starting with "/", like RentAll's own /payments/pay-request) are resolved against it.
    /// </summary>
    public string? PaymentPagesBaseUrl { get; set; }
    public string? EIndex { get; set; }
}

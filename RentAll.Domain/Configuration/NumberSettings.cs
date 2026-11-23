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

    /// <summary>The WidOptions block from Number's PayForm builder. Blank values fall back to Number's sample values.</summary>
    public NumberWidOptions WidOptions { get; set; } = new();
}

public class NumberWidOptions
{
    public string? EVisible { get; set; }
    public string? EReadOnly { get; set; }
    public string? EStyles { get; set; }
    public string? ESubmission { get; set; }
    public string? EFeatures { get; set; }
    public string? EColors { get; set; }
}

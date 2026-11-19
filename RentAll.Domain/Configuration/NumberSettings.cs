namespace RentAll.Domain.Configuration;

public class NumberSettings
{
    public string? KeyVaultUri { get; set; }
    public string AuthenticateUrl { get; set; } = string.Empty;
    public string InitFormUrl { get; set; } = string.Empty;
    public string TransactionDetailUrl { get; set; } = string.Empty;
    public string ConsentDetailUrl { get; set; } = string.Empty;
    public string ChargeStoredCardUrl { get; set; } = string.Empty;
    public string? PostUrl { get; set; }
    public string? RedirectUrl { get; set; }
    public string? EIndex { get; set; }

    /// <summary>
    /// True when the PayForm only saves the card (no charge): InitForm is sent with a $0 amount and
    /// staff charge the saved card later. Turn on together with the builder's Save Card on File WidOptions.
    /// </summary>
    public bool SaveCardOnly { get; set; }

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

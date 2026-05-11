namespace RentAll.Domain.Configuration;

public class SendGridSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? KeyVaultUri { get; set; }

    /// <summary>
    /// When true, SendGrid may rewrite links for click analytics (breaks long URLs in some clients).
    /// Default false so listing/share URLs stay unchanged.
    /// </summary>
    public bool EnableSendGridClickTracking { get; set; }
}

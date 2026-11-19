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

    public string? PaymentInformationRequestTemplateId { get; set; }

    /// <summary>
    /// Temporary testing override: when set, every SendGrid template email is sent only to this address.
    /// </summary>
    public string? TemplateEmailTestOverrideTo { get; set; }

    /// <summary>
    /// Temporary testing override: when set, every SendGrid template email uses this From address.
    /// </summary>
    public string? TemplateEmailTestOverrideFrom { get; set; }

    /// <summary>
    /// Optional display name for TemplateEmailTestOverrideFrom.
    /// </summary>
    public string? TemplateEmailTestOverrideFromName { get; set; }
}

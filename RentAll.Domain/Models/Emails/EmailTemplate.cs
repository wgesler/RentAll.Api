namespace RentAll.Domain.Models;

/// <summary>A SendGrid dynamic template ID for a named email, per organization and optionally per office.</summary>
public class EmailTemplate
{
    public int EmailTemplateId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string EmailName { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
}

/// <summary>EmailName values stored in Email.EmailTemplate: the emails that are sent with a SendGrid template.</summary>
public static class EmailTemplateNames
{
    public const string PaymentRequest = "PaymentRequest";

    /// <summary>Every name the settings screen offers, with its display label.</summary>
    public static readonly IReadOnlyDictionary<string, string> All = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [PaymentRequest] = "Payment Request"
    };

    public static bool IsKnown(string? emailName) => !string.IsNullOrWhiteSpace(emailName) && All.ContainsKey(emailName.Trim());

    /// <summary>The name exactly as stored, e.g. "paymentrequest" becomes "PaymentRequest".</summary>
    public static string Canonical(string emailName) => All.Keys.First(key => key.Equals(emailName.Trim(), StringComparison.OrdinalIgnoreCase));
}

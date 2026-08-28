namespace RentAll.Domain.Configuration;

public class DocuSignSettings
{
    public string? KeyVaultUri { get; set; }

    /// <summary>
    /// OAuth audience host, e.g. account-d.docusign.com (demo) or account.docusign.com (production).
    /// </summary>
    public string AuthServer { get; set; } = "account-d.docusign.com";

    /// <summary>
    /// REST API base URL, e.g. https://demo.docusign.net/restapi or https://na4.docusign.net/restapi.
    /// Used when BaseUri is not set.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://demo.docusign.net/restapi";

    /// <summary>
    /// Optional local override. When set, used instead of Key Vault secret docusign-client-id.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Optional local override. When set, used instead of Key Vault secret docusign-private-key.
    /// </summary>
    public string? PrivateKey { get; set; }

    /// <summary>
    /// Optional appsettings override for DocuSign userId when not supplied on the user record.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Optional appsettings override for DocuSign accountId when not supplied on the office record.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// DocuSign account base URI, e.g. https://na4.docusign.net.
    /// </summary>
    public string? BaseUri { get; set; }
}

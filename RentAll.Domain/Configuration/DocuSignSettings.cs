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
    /// Optional local override. When set, used instead of the tenant secret userId field.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Optional local override. When set, used instead of the tenant secret accountId field.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// Optional local override. When set, used instead of the tenant secret baseUri field.
    /// </summary>
    public string? BaseUri { get; set; }
}

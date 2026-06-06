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
}

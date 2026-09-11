namespace RentAll.Domain.Configuration;

public class DocumentIntelligenceSettings
{
    public bool Enabled { get; set; }

    /// <summary>
    /// Azure Document Intelligence endpoint, e.g. https://rentall-document-intelligence.cognitiveservices.azure.com/.
    /// When empty, <see cref="EndpointSecretName"/> is read from Key Vault.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Optional local override. When set, used instead of Key Vault secret <see cref="ApiKeySecretName"/>.
    /// </summary>
    public string? ApiKey { get; set; }

    public string? KeyVaultUri { get; set; }

    public string ApiKeySecretName { get; set; } = "document-intelligence-api-key";

    public string EndpointSecretName { get; set; } = "document-intelligence-endpoint";
}

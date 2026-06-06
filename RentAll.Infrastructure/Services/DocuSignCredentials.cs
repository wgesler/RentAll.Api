namespace RentAll.Infrastructure.Services;

internal class DocuSignCredentials
{
    // RentAll / Key Vault
    public string ClientId { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;

    // Tenant / Key Vault
    public string UserId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string BaseUri { get; set; } = string.Empty;
}

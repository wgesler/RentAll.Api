using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Infrastructure.Services;

public class ExternalApiKeyService : IExternalApiKeyService
{
    private readonly ExternalIntakeSettings _settings;

    public ExternalApiKeyService(IOptions<ExternalIntakeSettings> settings)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<string?> GetApiKeyAsync(string? keyVaultSecretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyVaultSecretName))
            return null;

        if (string.IsNullOrWhiteSpace(_settings.KeyVaultUri))
            throw new InvalidOperationException("ExternalIntakeSettings:KeyVaultUri is not set.");

        var client = new SecretClient(new Uri(_settings.KeyVaultUri), new DefaultAzureCredential());
        var secret = await client.GetSecretAsync(keyVaultSecretName, cancellationToken: cancellationToken);
        return secret.Value.Value;
    }

    public async Task<bool> IsApiKeyValidAsync(string? inboundApiKey, string? keyVaultSecretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inboundApiKey) || string.IsNullOrWhiteSpace(keyVaultSecretName))
            return false;

        try
        {
            var configuredApiKey = await GetApiKeyAsync(keyVaultSecretName, cancellationToken);
            if (string.IsNullOrWhiteSpace(configuredApiKey))
                return false;

            return string.Equals(inboundApiKey.Trim(), configuredApiKey.Trim(), StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}

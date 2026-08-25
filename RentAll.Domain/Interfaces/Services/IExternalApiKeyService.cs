namespace RentAll.Domain.Interfaces.Services;

public interface IExternalApiKeyService
{
    Task<string?> GetApiKeyAsync(string? keyVaultSecretName, CancellationToken cancellationToken = default);

    Task<bool> IsApiKeyValidAsync(string? inboundApiKey, string? keyVaultSecretName, CancellationToken cancellationToken = default);
}

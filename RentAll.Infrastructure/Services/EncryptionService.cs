using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace RentAll.Infrastructure.Services;

public class EncryptionService : IEncryptionService
{
    private const byte PayloadVersion = 1;
    private readonly EncryptionSettings _settings;
    private readonly ILogger<EncryptionService> _logger;
    private readonly SemaphoreSlim _keyLock = new(1, 1);

    private byte[]? _cachedKey;
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;

    public EncryptionService(
        IOptions<EncryptionSettings> settings,
        ILogger<EncryptionService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
    }

    public async Task<byte[]> EncryptAsync(string plainText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            throw new InvalidOperationException("Value is required for encryption.");

        var key = await GetKeyAsync(cancellationToken);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var payload = new byte[1 + nonce.Length + tag.Length + cipherBytes.Length];
        payload[0] = PayloadVersion;
        Buffer.BlockCopy(nonce, 0, payload, 1, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, 1 + nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, payload, 1 + nonce.Length + tag.Length, cipherBytes.Length);

        return payload;
    }

    public async Task<string> DecryptAsync(byte[] cipherText, CancellationToken cancellationToken = default)
    {
        if (cipherText == null || cipherText.Length < 1 + 12 + 16 + 1)
            throw new InvalidOperationException("Encrypted payload is invalid.");

        if (cipherText[0] != PayloadVersion)
            throw new InvalidOperationException("Unsupported encrypted payload version.");

        var key = await GetKeyAsync(cancellationToken);

        var nonce = cipherText.AsSpan(1, 12).ToArray();
        var tag = cipherText.AsSpan(13, 16).ToArray();
        var encryptedBytes = cipherText.AsSpan(29).ToArray();
        var plainBytes = new byte[encryptedBytes.Length];

        using var aes = new AesGcm(key, 16);
        aes.Decrypt(nonce, encryptedBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private async Task<byte[]> GetKeyAsync(CancellationToken cancellationToken)
    {
        if (_cachedKey != null && DateTimeOffset.UtcNow - _cachedAt < TimeSpan.FromMinutes(10))
            return _cachedKey;

        await _keyLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedKey != null && DateTimeOffset.UtcNow - _cachedAt < TimeSpan.FromMinutes(10))
                return _cachedKey;

            if (string.IsNullOrWhiteSpace(_settings.KeyVaultUri))
                throw new InvalidOperationException("EncryptionSettings:KeyVaultUri is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.SecretName))
                throw new InvalidOperationException("EncryptionSettings:SecretName is not configured.");

            var secretClient = new SecretClient(new Uri(_settings.KeyVaultUri), new DefaultAzureCredential());
            var keySecret = await secretClient.GetSecretAsync(_settings.SecretName, cancellationToken: cancellationToken);
            var rawSecret = keySecret.Value.Value;

            if (string.IsNullOrWhiteSpace(rawSecret))
                throw new InvalidOperationException("Encryption key secret is empty.");

            _cachedKey = SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret));
            _cachedAt = DateTimeOffset.UtcNow;
            return _cachedKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load encryption key from Key Vault.");
            throw;
        }
        finally
        {
            _keyLock.Release();
        }
    }
}

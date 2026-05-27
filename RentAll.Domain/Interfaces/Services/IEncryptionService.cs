namespace RentAll.Domain.Interfaces.Services;

public interface IEncryptionService
{
    Task<byte[]> EncryptAsync(string plainText, CancellationToken cancellationToken = default);
    Task<string> DecryptAsync(byte[] cipherText, CancellationToken cancellationToken = default);
}

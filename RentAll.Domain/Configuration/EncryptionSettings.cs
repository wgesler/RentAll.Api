namespace RentAll.Domain.Configuration;

public class EncryptionSettings
{
    public string? KeyVaultUri { get; set; }
    public string SecretName { get; set; } = "encryption-key";
}

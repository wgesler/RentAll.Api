using Microsoft.Data.SqlClient;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Contacts;

public partial class ContactRepository
{
    public async Task SaveEncryptedAccountNumberAsync(Guid contactId, byte[] encryptedAccountNumber, string lastFour, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.Contact_SaveEncryptedAccountNumber", new
        {
            ContactId = contactId,
            AccountNumberEncrypted = encryptedAccountNumber,
            AccountNumberLastFour = lastFour,
            ModifiedBy = modifiedBy
        });
    }

    public async Task<byte[]?> GetEncryptedAccountNumberAsync(Guid contactId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<EncryptedAccountNumberRow>("Organization.Contact_GetEncryptedAccountNumber", new
        {
            ContactId = contactId
        });

        return rows?.FirstOrDefault()?.AccountNumberEncrypted;
    }

    public async Task<IEnumerable<(Guid ContactId, string AccountNumber)>> GetClearAccountNumbersAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<ClearAccountNumberRow>("Organization.Contact_GetClearAccountNumbers", new
        {
            OrganizationId = organizationId
        });

        return (rows ?? []).Select(row => (row.ContactId, row.AccountNumber ?? string.Empty));
    }

    public async Task ClearBankInformationAsync(Guid contactId, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.Contact_ClearBankInformation", new
        {
            ContactId = contactId,
            ModifiedBy = modifiedBy
        });
    }

    private sealed class EncryptedAccountNumberRow
    {
        public byte[]? AccountNumberEncrypted { get; set; }
    }

    private sealed class ClearAccountNumberRow
    {
        public Guid ContactId { get; set; }
        public string? AccountNumber { get; set; }
    }
}

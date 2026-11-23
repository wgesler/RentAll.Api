using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Contacts;

public partial class ContactRepository
{
    #region ContactBanks
    public async Task<ContactBank?> GetContactBankByIdAsync(int contactBankId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ContactBankEntity>("Organization.ContactBank_GetById", new
        {
            ContactBankId = contactBankId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }

    public async Task<ContactBank> CreateContactBankAsync(ContactBank contactBank, byte[]? encryptedAccountNumber)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ContactBankEntity>("Organization.ContactBank_Add", new
        {
            OrganizationId = contactBank.OrganizationId,
            OfficeId = contactBank.OfficeId,
            BankAccountTypeId = contactBank.BankAccountTypeId,
            NameOnAccount = contactBank.NameOnAccount,
            BankName = contactBank.BankName,
            RoutingNumber = contactBank.RoutingNumber,
            AccountNumber = encryptedAccountNumber,
            LastFour = contactBank.LastFour,
            NumberConsentId = contactBank.NumberConsentId,
            ReservationId = contactBank.ReservationId,
            AuthorizedOn = contactBank.AuthorizedOn
        });

        if (res == null || !res.Any())
            throw new Exception("ContactBank not created");

        return ConvertEntityToModel(res.First());
    }

    public async Task<ContactBank> UpdateContactBankByIdAsync(ContactBank contactBank, byte[]? encryptedAccountNumber)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ContactBankEntity>("Organization.ContactBank_UpdateById", new
        {
            ContactBankId = contactBank.ContactBankId,
            OrganizationId = contactBank.OrganizationId,
            OfficeId = contactBank.OfficeId,
            BankAccountTypeId = contactBank.BankAccountTypeId,
            NameOnAccount = contactBank.NameOnAccount,
            BankName = contactBank.BankName,
            RoutingNumber = contactBank.RoutingNumber,
            AccountNumber = encryptedAccountNumber,
            LastFour = contactBank.LastFour,
            NumberConsentId = contactBank.NumberConsentId,
            ReservationId = contactBank.ReservationId,
            AuthorizedOn = contactBank.AuthorizedOn
        });

        if (res == null || !res.Any())
            throw new Exception("ContactBank not found");

        return ConvertEntityToModel(res.First());
    }

    public async Task DeleteContactBankByIdAsync(int contactBankId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.ContactBank_DeleteById", new
        {
            ContactBankId = contactBankId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });
    }

    private static ContactBank ConvertEntityToModel(ContactBankEntity e)
    {
        return new ContactBank
        {
            ContactBankId = e.ContactBankId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            BankAccountTypeId = e.BankAccountTypeId,
            NameOnAccount = e.NameOnAccount,
            BankName = e.BankName,
            RoutingNumber = e.RoutingNumber,
            // Base64 of the encrypted bytes; empty when no account number is stored.
            AccountNumber = e.AccountNumber is { Length: > 0 } ? Convert.ToBase64String(e.AccountNumber) : string.Empty,
            LastFour = e.LastFour,
            NumberConsentId = e.NumberConsentId,
            ReservationId = e.ReservationId,
            AuthorizedOn = e.AuthorizedOn,
            CreatedOn = e.CreatedOn
        };
    }
    #endregion
}

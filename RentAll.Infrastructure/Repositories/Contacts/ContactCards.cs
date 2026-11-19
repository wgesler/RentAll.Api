using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Contacts;

public partial class ContactRepository
{
    #region ContactCards
    public async Task<ContactCard?> GetContactCardByIdAsync(int contactCardId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ContactCardEntity>("Organization.ContactCard_GetById", new
        {
            ContactCardId = contactCardId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }

    // encryptedCardNumber is null when the card is stored at the payment processor.
    public async Task<ContactCard> CreateContactCardAsync(ContactCard contactCard, byte[]? encryptedCardNumber)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ContactCardEntity>("Organization.ContactCard_Add", new
        {
            OrganizationId = contactCard.OrganizationId,
            OfficeId = contactCard.OfficeId,
            CardTypeId = contactCard.CardTypeId,
            CardName = contactCard.CardName,
            CardNumber = encryptedCardNumber,
            LastFour = contactCard.LastFour,
            ExpirationDate = contactCard.ExpirationDate,
            NumberConsentId = contactCard.NumberConsentId,
            IsExternallyStored = contactCard.IsExternallyStored
        });

        if (res == null || !res.Any())
            throw new Exception("ContactCard not created");

        return ConvertEntityToModel(res.First());
    }

    public async Task<ContactCard> UpdateContactCardByIdAsync(ContactCard contactCard, byte[]? encryptedCardNumber)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ContactCardEntity>("Organization.ContactCard_UpdateById", new
        {
            ContactCardId = contactCard.ContactCardId,
            OrganizationId = contactCard.OrganizationId,
            OfficeId = contactCard.OfficeId,
            CardTypeId = contactCard.CardTypeId,
            CardName = contactCard.CardName,
            CardNumber = encryptedCardNumber,
            LastFour = contactCard.LastFour,
            ExpirationDate = contactCard.ExpirationDate,
            NumberConsentId = contactCard.NumberConsentId,
            IsExternallyStored = contactCard.IsExternallyStored
        });

        if (res == null || !res.Any())
            throw new Exception("ContactCard not found");

        return ConvertEntityToModel(res.First());
    }

    public async Task DeleteContactCardByIdAsync(int contactCardId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.ContactCard_DeleteById", new
        {
            ContactCardId = contactCardId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });
    }

    private static ContactCard ConvertEntityToModel(ContactCardEntity e)
    {
        return new ContactCard
        {
            ContactCardId = e.ContactCardId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            CardTypeId = e.CardTypeId,
            CardName = e.CardName,
            DisplayName = e.DisplayName,
            // Base64 of the encrypted bytes; empty when the card is stored externally.
            CardNumber = e.CardNumber is { Length: > 0 } ? Convert.ToBase64String(e.CardNumber) : string.Empty,
            LastFour = e.LastFour,
            ExpirationDate = e.ExpirationDate,
            NumberConsentId = e.NumberConsentId,
            IsExternallyStored = e.IsExternallyStored,
            ChartOfAccountId = e.ChartOfAccountId
        };
    }
    #endregion
}

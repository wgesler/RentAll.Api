using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Contacts;

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

        var entity = res.First();
        var card = ConvertEntityToModel(entity);
        card.CardNumber = Convert.ToBase64String(entity.CardNumber ?? []);
        return card;
    }

    public async Task<ContactCard> CreateContactCardAsync(ContactCard contactCard, byte[] encryptedCardNumber)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ContactCardEntity>("Organization.ContactCard_Add", new
        {
            OrganizationId = contactCard.OrganizationId,
            OfficeId = contactCard.OfficeId,
            CardTypeId = contactCard.CardTypeId,
            CardName = contactCard.CardName,
            CardNumber = encryptedCardNumber,
            LastFour = contactCard.LastFour
        });

        if (res == null || !res.Any())
            throw new Exception("ContactCard not created");

        var entity = res.First();
        var card = ConvertEntityToModel(entity);
        card.CardNumber = Convert.ToBase64String(entity.CardNumber ?? []);
        return card;
    }

    public async Task<ContactCard> UpdateContactCardByIdAsync(ContactCard contactCard, byte[] encryptedCardNumber)
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
            LastFour = contactCard.LastFour
        });

        if (res == null || !res.Any())
            throw new Exception("ContactCard not found");

        var entity = res.First();
        var card = ConvertEntityToModel(entity);
        card.CardNumber = Convert.ToBase64String(entity.CardNumber ?? []);
        return card;
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
            LastFour = e.LastFour,
            ChartOfAccountId = e.ChartOfAccountId
        };
    }
    #endregion
}

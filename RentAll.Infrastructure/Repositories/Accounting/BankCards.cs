using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Selects
    public async Task<List<BankCard>> GetBankCardsByOfficeIdAsync(Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankCardEntity>("Accounting.BankCard_GetAllByOfficeId", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return new List<BankCard>();

        return res.Select(e =>
        {
            var card = ConvertEntityToModel(e);
            card.CardNumber = Convert.ToBase64String(e.CardNumber ?? []);
            return card;
        }).ToList();
    }

    public async Task<List<BankCard>> GetBankCardsByOfficeIdsAsync(Guid organizationId, string officeIds)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankCardEntity>("Accounting.BankCard_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeIds
        });

        if (res == null || !res.Any())
            return new List<BankCard>();

        return res.Select(e =>
        {
            var card = ConvertEntityToModel(e);
            card.CardNumber = Convert.ToBase64String(e.CardNumber ?? []);
            return card;
        }).ToList();
    }

    public async Task<BankCard?> GetBankCardByIdAsync(int bankCardId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankCardEntity>("Accounting.BankCard_GetById", new
        {
            BankCardId = bankCardId,
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
    #endregion

    #region Creates
    public async Task<BankCard> CreateAsync(BankCard bankCard, byte[] encryptedCardNumber)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankCardEntity>("Accounting.BankCard_Add", new
        {
            OrganizationId = bankCard.OrganizationId,
            OfficeId = bankCard.OfficeId,
            CardTypeId = bankCard.CardTypeId,
            CardName = bankCard.CardName,
            CardNumber = encryptedCardNumber,
            LastFour = bankCard.LastFour,
            ChartOfAccountId = bankCard.ChartOfAccountId
        });

        if (res == null || !res.Any())
            throw new Exception("BankCard not created");

        var entity = res.First();
        var card = ConvertEntityToModel(entity);
        card.CardNumber = Convert.ToBase64String(entity.CardNumber ?? []);
        return card;
    }
    #endregion

    #region Updates
    public async Task<BankCard> UpdateByIdAsync(BankCard bankCard, byte[] encryptedCardNumber)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankCardEntity>("Accounting.BankCard_UpdateById", new
        {
            BankCardId = bankCard.BankCardId,
            OrganizationId = bankCard.OrganizationId,
            OfficeId = bankCard.OfficeId,
            CardTypeId = bankCard.CardTypeId,
            CardName = bankCard.CardName,
            CardNumber = encryptedCardNumber,
            LastFour = bankCard.LastFour,
            ChartOfAccountId = bankCard.ChartOfAccountId
        });

        if (res == null || !res.Any())
            throw new Exception("BankCard not found");

        var entity = res.First();
        var card = ConvertEntityToModel(entity);
        card.CardNumber = Convert.ToBase64String(entity.CardNumber ?? []);
        return card;
    }
    #endregion

    #region Deletes
    public async Task DeleteBankCardByIdAsync(int bankCardId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.BankCard_DeleteById", new
        {
            BankCardId = bankCardId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });
    }
    #endregion
}

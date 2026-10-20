using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Selects
    public async Task<List<Bank>> GetBanksByOfficeIdAsync(Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankEntity>("Accounting.BankAccount_GetAllByOfficeId", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return new List<Bank>();

        return res.Select(ConvertEntityToModel).ToList();
    }

    public async Task<Bank?> GetBankByIdAsync(int bankId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankEntity>("Accounting.BankAccount_GetById", new
        {
            BankId = bankId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Creates
    public async Task<Bank> CreateBankAsync(Bank bank)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankEntity>("Accounting.BankAccount_Add", new
        {
            OrganizationId = bank.OrganizationId,
            OfficeId = bank.OfficeId,
            BankName = bank.BankName,
            BankPhone = bank.BankPhone,
            BankRouting = bank.BankRouting,
            BankAccount = bank.BankAccount,
            BankSwiftCode = bank.BankSwiftCode,
            BankAddress = bank.BankAddress,
            CurrentCheckNumber = bank.CurrentCheckNumber,
            CheckStockPath = bank.CheckStockPath,
            CheckHtml = bank.CheckHtml,
            CreatedBy = bank.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Bank not created");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Updates
    public async Task<Bank> UpdateBankByIdAsync(Bank bank)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankEntity>("Accounting.BankAccount_UpdateById", new
        {
            BankId = bank.BankId,
            OrganizationId = bank.OrganizationId,
            OfficeId = bank.OfficeId,
            BankName = bank.BankName,
            BankPhone = bank.BankPhone,
            BankRouting = bank.BankRouting,
            BankAccount = bank.BankAccount,
            BankSwiftCode = bank.BankSwiftCode,
            BankAddress = bank.BankAddress,
            CurrentCheckNumber = bank.CurrentCheckNumber,
            CheckStockPath = bank.CheckStockPath,
            CheckHtml = bank.CheckHtml,
            ModifiedBy = bank.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Bank not found");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Deletes
    public async Task DeleteBankByIdAsync(int bankId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.BankAccount_DeleteById", new
        {
            BankId = bankId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });
    }
    #endregion
}

using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Selects
    public async Task<List<BankAccount>> GetBankAccountsByOfficeIdAsync(Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankAccountEntity>("Accounting.BankAccount_GetAllByOfficeId", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return new List<BankAccount>();

        return res.Select(ConvertEntityToModel).ToList();
    }

    public async Task<BankAccount?> GetBankAccountByIdAsync(int bankId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankAccountEntity>("Accounting.BankAccount_GetById", new
        {
            BankAccountId = bankId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Creates
    public async Task<BankAccount> CreateBankAccountAsync(BankAccount bankAccount)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankAccountEntity>("Accounting.BankAccount_Add", new
        {
            OrganizationId = bankAccount.OrganizationId,
            OfficeId = bankAccount.OfficeId,
            BankName = bankAccount.BankName,
            BankPhone = bankAccount.BankPhone,
            BankRouting = bankAccount.BankRouting,
            BankSwiftCode = bankAccount.BankSwiftCode,
            BankAddress = bankAccount.BankAddress,
            AccountChartOfAccountId = bankAccount.AccountChartOfAccountId,
            CurrentCheckNumber = bankAccount.CurrentCheckNumber,
            CheckStockPath = bankAccount.CheckStockPath,
            CheckHtml = bankAccount.CheckHtml,
            CreatedBy = bankAccount.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Bank account not created");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Updates
    public async Task<BankAccount> UpdateBankAccountByIdAsync(BankAccount bankAccount)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BankAccountEntity>("Accounting.BankAccount_UpdateById", new
        {
            BankAccountId = bankAccount.BankAccountId,
            OrganizationId = bankAccount.OrganizationId,
            OfficeId = bankAccount.OfficeId,
            BankName = bankAccount.BankName,
            BankPhone = bankAccount.BankPhone,
            BankRouting = bankAccount.BankRouting,
            BankSwiftCode = bankAccount.BankSwiftCode,
            BankAddress = bankAccount.BankAddress,
            AccountChartOfAccountId = bankAccount.AccountChartOfAccountId,
            CurrentCheckNumber = bankAccount.CurrentCheckNumber,
            CheckStockPath = bankAccount.CheckStockPath,
            CheckHtml = bankAccount.CheckHtml,
            ModifiedBy = bankAccount.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Bank account not found");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Deletes
    public async Task DeleteBankAccountByIdAsync(int bankId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.BankAccount_DeleteById", new
        {
            BankAccountId = bankId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });
    }

    public async Task SaveEncryptedBankAccountAsync(int bankId, Guid organizationId, int officeId, byte[] encryptedBankAccount, string lastFour, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.BankAccount_SaveEncryptedBankAccount", new
        {
            BankAccountId = bankId,
            OrganizationId = organizationId,
            OfficeId = officeId,
            AccountNo = encryptedBankAccount,
            AccountNoLastFour = lastFour,
            ModifiedBy = modifiedBy
        });
    }

    public async Task<byte[]?> GetEncryptedBankAccountAsync(int bankId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<EncryptedBankAccountRow>("Accounting.BankAccount_GetEncryptedBankAccount", new
        {
            BankAccountId = bankId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });
        return rows?.FirstOrDefault()?.AccountNo;
    }

    private sealed class EncryptedBankAccountRow
    {
        public byte[]? AccountNo { get; set; }
    }
    #endregion
}

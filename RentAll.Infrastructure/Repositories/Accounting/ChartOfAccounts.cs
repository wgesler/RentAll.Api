using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Selects
    public async Task<List<ChartOfAccount>> GetChartOfAccountsByOfficeIdsAsync(Guid organizationId, string officeIds)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccounts_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeIds
        });

        if (res == null || !res.Any())
            return new List<ChartOfAccount>();

        return res.Select(ConvertEntityToModel).ToList();
    }

    public async Task<List<ChartOfAccount>> GetChartOfAccountsByOfficeIdAsync(Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccounts_GetAllByOfficeId", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return new List<ChartOfAccount>();

        return res.Select(ConvertEntityToModel).ToList();
    }

    public async Task<ChartOfAccount?> GetChartOfAccountByIdAsync(Guid organizationId, int officeId, int accountId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccounts_GetById", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            AccountId = accountId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<ChartOfAccount?> GetChartOfAccountByAccountNoAsync(Guid organizationId, int officeId, string accountNo)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccounts_GetByAccountNo", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            AccountNo = accountNo
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<bool> ExistsChartOfAccountByAccountIdAsync(Guid organizationId, int officeId, int accountId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryScalarAsync<int>("Accounting.ChartOfAccounts_ExistsByAccountId", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            AccountId = accountId
        });

        return result == 1;
    }

    public async Task<bool> ExistsChartOfAccountByAccountNoAsync(Guid organizationId, int officeId, string accountNo, int? excludeAccountId = null)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryScalarAsync<int>("Accounting.ChartOfAccounts_ExistsByAccountNo", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            AccountNo = accountNo,
            ExcludeAccountId = excludeAccountId
        });

        return result == 1;
    }
    #endregion

    #region Creates
    public async Task<ChartOfAccount> CreateAsync(ChartOfAccount chartOfAccount)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccounts_Add", new
        {
            OrganizationId = chartOfAccount.OrganizationId,
            OfficeId = chartOfAccount.OfficeId,
            AccountNo = chartOfAccount.AccountNo,
            AccountTypeId = (int)chartOfAccount.AccountType,
            Name = chartOfAccount.Name,
            IsSubaccount = chartOfAccount.IsSubaccount,
            SubAccountId = chartOfAccount.SubAccountId,
            Description = chartOfAccount.Description,
            Note = chartOfAccount.Note
        });

        if (res == null || !res.Any())
            throw new Exception("ChartOfAccount not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
    public async Task<ChartOfAccount> UpdateChartOfAccountByIdAsync(ChartOfAccount chartOfAccount)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccounts_UpdateById", new
        {
            OrganizationId = chartOfAccount.OrganizationId,
            OfficeId = chartOfAccount.OfficeId,
            AccountId = chartOfAccount.AccountId,
            AccountNo = chartOfAccount.AccountNo,
            AccountTypeId = (int)chartOfAccount.AccountType,
            Name = chartOfAccount.Name,
            IsSubaccount = chartOfAccount.IsSubaccount,
            SubAccountId = chartOfAccount.SubAccountId,
            Description = chartOfAccount.Description,
            Note = chartOfAccount.Note
        });

        if (res == null || !res.Any())
            throw new Exception("ChartOfAccount not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Deletes
    public async Task DeleteChartOfAccountByIdAsync(Guid organizationId, int officeId, int accountId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.ChartOfAccounts_DeleteById", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            AccountId = accountId
        });
    }
    #endregion
}

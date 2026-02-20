using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Create
    public async Task<AccountingOffice> CreateAccountingAsync(AccountingOffice accountingOffice)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Accounting.Office_Add", new
        {
            OrganizationId = accountingOffice.OrganizationId,
            OfficeId = accountingOffice.OfficeId,
            Name = accountingOffice.Name,
            Address1 = accountingOffice.Address1,
            Address2 = accountingOffice.Address2,
            Suite = accountingOffice.Suite,
            City = accountingOffice.City,
            State = accountingOffice.State,
            Zip = accountingOffice.Zip,
            Phone = accountingOffice.Phone,
            Fax = accountingOffice.Fax,
            Email = accountingOffice.Email,
            Website = accountingOffice.Website,
            BankName = accountingOffice.BankName,
            BankRouting = accountingOffice.BankRouting,
            BankAccount = accountingOffice.BankAccount,
            BankSwiftCode = accountingOffice.BankSwiftCode,
            BankAddress = accountingOffice.BankAddress,
            BankPhone = accountingOffice.BankPhone,
            LogoPath = accountingOffice.LogoPath,
            IsActive = accountingOffice.IsActive,
            CreatedBy = accountingOffice.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("AccountingOffice not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Select
    public async Task<IEnumerable<AccountingOffice>> GetAllAccountingByOfficeIdAsync(Guid organizationId, string officeIds)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Accounting.Office_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeIds
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<AccountingOffice>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<AccountingOffice?> GetAccountingByIdAsync(Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Accounting.Office_GetById", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Update
    public async Task<AccountingOffice> UpdateAccountingAsync(AccountingOffice accountingOffice)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Accounting.Office_UpdateById", new
        {
            OrganizationId = accountingOffice.OrganizationId,
            OfficeId = accountingOffice.OfficeId,
            Name = accountingOffice.Name,
            Address1 = accountingOffice.Address1,
            Address2 = accountingOffice.Address2,
            Suite = accountingOffice.Suite,
            City = accountingOffice.City,
            State = accountingOffice.State,
            Zip = accountingOffice.Zip,
            Phone = accountingOffice.Phone,
            Fax = accountingOffice.Fax,
            Email = accountingOffice.Email,
            Website = accountingOffice.Website,
            BankName = accountingOffice.BankName,
            BankRouting = accountingOffice.BankRouting,
            BankAccount = accountingOffice.BankAccount,
            BankSwiftCode = accountingOffice.BankSwiftCode,
            BankAddress = accountingOffice.BankAddress,
            BankPhone = accountingOffice.BankPhone,
            LogoPath = accountingOffice.LogoPath,
            IsActive = accountingOffice.IsActive,
            ModifiedBy = accountingOffice.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("AccountingOffice not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Delete
    public async Task DeleteAccountingAsync(Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.Office_Delete", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });
    }
    #endregion
}

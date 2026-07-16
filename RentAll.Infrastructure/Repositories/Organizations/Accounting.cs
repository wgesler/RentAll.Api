using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Selects
    public async Task<IEnumerable<AccountingOffice>> GetAccountingOfficesByOfficeIdsAsync(Guid organizationId, string officeIds)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Organization.AccountingOffice_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeIds
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<AccountingOffice>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<AccountingOffice>> GetAccountingOfficesByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Organization.AccountingOffice_GetAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<AccountingOffice>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<AccountingOffice?> GetAccountingOfficeByIdAsync(Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Organization.AccountingOffice_GetById", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Creates
    public async Task<AccountingOffice> CreateAccountingAsync(AccountingOffice accountingOffice)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Organization.AccountingOffice_Add", new
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
            YearEndMonth = accountingOffice.YearEndMonth,
            YearEndDay = accountingOffice.YearEndDay,
            WorkOrderNo = accountingOffice.WorkOrderNo,
            DefaultTenantIncAccountId = accountingOffice.DefaultTenantIncAccountId,
            DefaultTenantExpAccountId = accountingOffice.DefaultTenantExpAccountId,
            DefaultOwnerIncAccountId = accountingOffice.DefaultOwnerIncAccountId,
            DefaultOwnerExpAccountId = accountingOffice.DefaultOwnerExpAccountId,
            DefaultCompanyExpAccountId = accountingOffice.DefaultCompanyExpAccountId,
            DefaultPmUtilityIncAccountId = accountingOffice.DefaultPmUtilityIncAccountId,
            DefaultLaborIncAccountId = accountingOffice.DefaultLaborIncAccountId,
            DefaultLinenTowelIncAccountId = accountingOffice.DefaultLinenTowelIncAccountId,
            DefaultDepartureIncAccountId = accountingOffice.DefaultDepartureIncAccountId,
            DefaultDepartureExpAccountId = accountingOffice.DefaultDepartureExpAccountId,
            DefaultBankAccountId = accountingOffice.DefaultBankAccountId,
            DefaultActRcvableAccountId = accountingOffice.DefaultActRcvableAccountId,
            DefaultActPayableAccountId = accountingOffice.DefaultActPayableAccountId,
            DefaultUndepFundsAccountId = accountingOffice.DefaultUndepFundsAccountId,
            DefaultEscrowDepositAccountId = accountingOffice.DefaultEscrowDepositAccountId,
            DefaultEscrowOwnersAccountId = accountingOffice.DefaultEscrowOwnersAccountId,
            DefaultEscrowSecDepAccountId = accountingOffice.DefaultEscrowSecDepAccountId,
            DefaultEscrowSdwAccountId = accountingOffice.DefaultEscrowSdwAccountId,
            DefaultOwnActPayableAccountId = accountingOffice.DefaultOwnActPayableAccountId,
            DefaultPrePayAccountId = accountingOffice.DefaultPrePayAccountId,
            DefaultRetainedEarningsAccountId = accountingOffice.DefaultRetainedEarningsAccountId,
            LogoPath = accountingOffice.LogoPath,
            CurrentCheckNumber = accountingOffice.CurrentCheckNumber,
            IsActive = accountingOffice.IsActive,
            CreatedBy = accountingOffice.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("AccountingOffice not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
    public async Task<AccountingOffice> UpdateAccountingAsync(AccountingOffice accountingOffice)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Organization.AccountingOffice_UpdateById", new
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
            YearEndMonth = accountingOffice.YearEndMonth,
            YearEndDay = accountingOffice.YearEndDay,
            WorkOrderNo = accountingOffice.WorkOrderNo,
            DefaultTenantIncAccountId = accountingOffice.DefaultTenantIncAccountId,
            DefaultTenantExpAccountId = accountingOffice.DefaultTenantExpAccountId,
            DefaultOwnerIncAccountId = accountingOffice.DefaultOwnerIncAccountId,
            DefaultOwnerExpAccountId = accountingOffice.DefaultOwnerExpAccountId,
            DefaultCompanyExpAccountId = accountingOffice.DefaultCompanyExpAccountId,
            DefaultPmUtilityIncAccountId = accountingOffice.DefaultPmUtilityIncAccountId,
            DefaultLaborIncAccountId = accountingOffice.DefaultLaborIncAccountId,
            DefaultLinenTowelIncAccountId = accountingOffice.DefaultLinenTowelIncAccountId,
            DefaultDepartureIncAccountId = accountingOffice.DefaultDepartureIncAccountId,
            DefaultDepartureExpAccountId = accountingOffice.DefaultDepartureExpAccountId,
            DefaultBankAccountId = accountingOffice.DefaultBankAccountId,
            DefaultActRcvableAccountId = accountingOffice.DefaultActRcvableAccountId,
            DefaultActPayableAccountId = accountingOffice.DefaultActPayableAccountId,
            DefaultUndepFundsAccountId = accountingOffice.DefaultUndepFundsAccountId,
            DefaultEscrowDepositAccountId = accountingOffice.DefaultEscrowDepositAccountId,
            DefaultEscrowOwnersAccountId = accountingOffice.DefaultEscrowOwnersAccountId,
            DefaultEscrowSecDepAccountId = accountingOffice.DefaultEscrowSecDepAccountId,
            DefaultEscrowSdwAccountId = accountingOffice.DefaultEscrowSdwAccountId,
            DefaultOwnActPayableAccountId = accountingOffice.DefaultOwnActPayableAccountId,
            DefaultPrePayAccountId = accountingOffice.DefaultPrePayAccountId,
            DefaultRetainedEarningsAccountId = accountingOffice.DefaultRetainedEarningsAccountId,
            LogoPath = accountingOffice.LogoPath,
            CurrentCheckNumber = accountingOffice.CurrentCheckNumber,
            IsActive = accountingOffice.IsActive,
            ModifiedBy = accountingOffice.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("AccountingOffice not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<AccountingOffice> UpdateAccountingOfficeWorkOrderNoByIdAsync(Guid organizationId, int officeId, int workOrderNo, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Organization.AccountingOffice_UpdateWorkOrderNoById", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            WorkOrderNo = workOrderNo,
            ModifiedBy = modifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("AccountingOffice not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<AccountingOffice> UpdateAccountingOfficeCheckNumberByIdAsync(Guid organizationId, int officeId, int currentCheckNumber, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Organization.AccountingOffice_UpdateCheckNumberById", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            CurrentCheckNumber = currentCheckNumber,
            ModifiedBy = modifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("AccountingOffice not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<AccountingOffice> UpdateAccountingOfficeCheckStockByIdAsync(Guid organizationId, int officeId, string? checkStockPath, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AccountingOfficeEntity>("Organization.AccountingOffice_UpdateCheckStockById", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            CheckStockPath = checkStockPath,
            ModifiedBy = modifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("AccountingOffice not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Deletes
    public async Task DeleteAccountingOfficeByIdAsync(Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.AccountingOffice_DeleteById", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });
    }
    #endregion
}

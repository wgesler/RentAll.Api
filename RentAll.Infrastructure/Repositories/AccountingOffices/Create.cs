using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.AccountingOffices;

public partial class AccountingOfficeRepository : IAccountingOfficeRepository
{
	public async Task<AccountingOffice> CreateAsync(AccountingOffice accountingOffice)
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
			BankName = accountingOffice.BankName,
			BankRouting = accountingOffice.BankRouting,
			BankAccount = accountingOffice.BankAccount,
			BankSwiftCode = accountingOffice.BankSwiftCode,
			BankAddress = accountingOffice.BankAddress,
			BankPhone = accountingOffice.BankPhone,
			Email = accountingOffice.Email,
			LogoPath = accountingOffice.LogoPath,
			IsActive = accountingOffice.IsActive,
			CreatedBy = accountingOffice.CreatedBy
		});

		if (res == null || !res.Any())
			throw new Exception("AccountingOffice not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

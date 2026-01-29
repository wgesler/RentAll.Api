using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.AccountingOffices;

public partial class AccountingOfficeRepository : IAccountingOfficeRepository
{
	private readonly string _dbConnectionString;

	public AccountingOfficeRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private AccountingOffice ConvertEntityToModel(AccountingOfficeEntity e)
	{
		return new AccountingOffice
		{
			OrganizationId = e.OrganizationId,
			OfficeId = e.OfficeId,
			Name = e.Name,
			Address1 = e.Address1,
			Address2 = e.Address2,
			Suite = e.Suite,
			City = e.City,
			State = e.State,
			Zip = e.Zip,
			Phone = e.Phone,
			Fax = e.Fax,
			BankName = e.BankName,
			BankRouting = e.BankRouting,
			BankAccount = e.BankAccount,
			BankSwiftCode = e.BankSwiftCode,
			BankAddress = e.BankAddress,
			BankPhone = e.BankPhone,
			Email = e.Email,
			LogoPath = e.LogoPath,
			IsActive = e.IsActive,
			CreatedOn = e.CreatedOn,
			CreatedBy = e.CreatedBy,
			ModifiedOn = e.ModifiedOn,
			ModifiedBy = e.ModifiedBy
		};
	}
}

using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.ChartOfAccounts;

public partial class ChartOfAccountRepository : IChartOfAccountRepository
{
	private readonly string _dbConnectionString;

	public ChartOfAccountRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private ChartOfAccount ConvertEntityToModel(ChartOfAccountEntity e)
	{
		return new ChartOfAccount
		{
			ChartOfAccountId = e.ChartOfAccountId,
			OrganizationId = e.OrganizationId,
			AccountNumber = e.AccountNumber,
			Description = e.Description,
			AccountType = (AccountType)e.AccountTypeId
		};
	}
}

using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.CostCodes;

public partial class CostCodeRepository : ICostCodeRepository
{
	private readonly string _dbConnectionString;

	public CostCodeRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private CostCode ConvertEntityToModel(CostCodeEntity e)
	{
		return new CostCode
		{
			CostCodeId = e.CostCodeId,
			OrganizationId = e.OrganizationId,
			OfficeId = e.OfficeId,
			Code = e.CostCode,
			TransactionType = (TransactionType)e.TransactionTypeId,
			Description = e.Description,
			IsActive = e.IsActive
		};
	}
}

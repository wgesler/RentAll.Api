using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Franchises;

public partial class FranchiseRepository : IFranchiseRepository
{
	private readonly string _dbConnectionString;

	public FranchiseRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private Franchise ConvertEntityToModel(FranchiseEntity e)
	{
		return new Franchise
		{
			OrganizationId = e.OrganizationId,
			FranchiseId = e.FranchiseId,
			FranchiseCode = e.FranchiseCode,
			Description = e.Description,
			IsActive = e.IsActive
		};
	}
}


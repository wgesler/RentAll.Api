using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Regions;

public partial class RegionRepository : IRegionRepository
{
	private readonly string _dbConnectionString;

	public RegionRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private Region ConvertEntityToModel(RegionEntity e)
	{
		return new Region
		{
			OrganizationId = e.OrganizationId,
			RegionId = e.RegionId,
			OfficeId = e.OfficeId,
			RegionCode = e.RegionCode,
			Name = e.Name,
			Description = e.Description,
			IsActive = e.IsActive
		};
	}
}


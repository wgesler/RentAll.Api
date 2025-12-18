using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Areas;

public partial class AreaRepository : IAreaRepository
{
	private readonly string _dbConnectionString;

	public AreaRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private Area ConvertEntityToModel(AreaEntity e)
	{
		return new Area
		{
			OrganizationId = e.OrganizationId,
			AreaId = e.AreaId,
			AreaCode = e.AreaCode,
			Description = e.Description,
			IsActive = e.IsActive
		};
	}
}


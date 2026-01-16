using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Buildings;

public partial class BuildingRepository : IBuildingRepository
{
	private readonly string _dbConnectionString;

	public BuildingRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private Building ConvertEntityToModel(BuildingEntity e)
	{
		return new Building
		{
			OrganizationId = e.OrganizationId,
			BuildingId = e.BuildingId,
			OfficeId = e.OfficeId,
			OfficeName = e.OfficeName,
			BuildingCode = e.BuildingCode,
			Name = e.Name,
			Description = e.Description,
			HoaName = e.HoaName,
			HoaPhone = e.HoaPhone,
			HoaEmail = e.HoaEmail,
			IsActive = e.IsActive
		};
	}
}


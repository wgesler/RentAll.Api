using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Colors;

public partial class ColorRepository : IColorRepository
{
	private readonly string _dbConnectionString;

	public ColorRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private Colour ConvertEntityToModel(ColorEntity e)
	{
		return new Colour
		{
			ColorId = e.ColorId,
			OrganizationId = e.OrganizationId,
			ReservationStatusId = e.ReservationStatusId,
			Color = e.Color
		};
	}
}


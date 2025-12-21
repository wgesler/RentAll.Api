using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.PropertySelections;

public partial class PropertySelectionRepository : IPropertySelectionRepository
{
	private readonly string _dbConnectionString;

	public PropertySelectionRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o =>
			o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private static PropertySelection ConvertEntityToModel(PropertySelectionEntity e)
	{
		return new PropertySelection
		{
			UserId = e.UserId,
			FromBeds = e.FromBeds,
			ToBeds = e.ToBeds,
			Accomodates = e.Accomodates,
			MaxRent = e.MaxRent,
			PropertyCode = e.PropertyCode,
			City = e.City,
			State = e.State,
			Unfurnished = e.Unfurnished,
			Cable = e.Cable,
			Streaming = e.Streaming,
			Pool = e.Pool,
			Jacuzzi = e.Jacuzzi,
			Security = e.Security,
			Parking = e.Parking,
			Pets = e.Pets,
			Smoking = e.Smoking,
			HighSpeedInternet = e.HighSpeedInternet,
			PropertyStatusId = e.PropertyStatusId,
			FranchiseCode = e.FranchiseCode,
			BuildingCode = e.BuildingCode,
			RegionCode = e.RegionCode,
			AreaCode = e.AreaCode
		};
	}
}



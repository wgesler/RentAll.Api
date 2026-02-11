using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.PropertySelections;

public partial class PropertySelectionRepository : IPropertySelectionRepository
{
	public async Task<PropertySelection> UpsertAsync(PropertySelection s)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<PropertySelectionEntity>("Property.PropertySelection_UpsertByUserId", new
		{
			UserId = s.UserId,
			FromBeds = s.FromBeds,
			ToBeds = s.ToBeds,
			Accomodates = s.Accomodates,
			MaxRent = s.MaxRent,
			PropertyCode = s.PropertyCode,
			City = s.City,
			State = s.State,
			Unfurnished = s.Unfurnished,
			Cable = s.Cable,
			Streaming = s.Streaming,
			Pool = s.Pool,
			Jacuzzi = s.Jacuzzi,
			Security = s.Security,
			Parking = s.Parking,
			Pets = s.Pets,
			Smoking = s.Smoking,
			HighSpeedInternet = s.HighSpeedInternet,
			PropertyStatusId = s.PropertyStatusId,
			OfficeCode = s.OfficeCode,
			BuildingCode = s.BuildingCode,
			RegionCode = s.RegionCode,
			AreaCode = s.AreaCode,
		});

		if (res == null || !res.Any())
			throw new Exception("Property selection not saved");

		return ConvertEntityToModel(res.First()!);
	}
}



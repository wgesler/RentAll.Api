using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Offices;

public partial class OfficeRepository : IOfficeRepository
{
	private readonly string _dbConnectionString;

	public OfficeRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private Office ConvertEntityToModel(OfficeEntity e)
	{
		return new Office
		{
			OrganizationId = e.OrganizationId,
			OfficeId = e.OfficeId,
			OfficeCode = e.OfficeCode,
			Name = e.Name,
			Address1 = e.Address1,
			Address2 = e.Address2,
			Suite = e.Suite,
			City = e.City,
			State = e.State,
			Zip = e.Zip,
			Phone = e.Phone,
			Fax = e.Fax,
			Website = e.Website,
			LogoPath = e.LogoPath,
			MaintenanceEmail = e.MaintenanceEmail,
			AfterHoursPhone = e.AfterHoursPhone,
			AfterHoursInstructions = e.AfterHoursInstructions,
			DaysAfterDeparture = e.DaysAfterDeparture,
			DefaultDeposit = e.DefaultDeposit,
			DefaultSdw = e.DefaultSdw,
			DefaultKeyFee = e.DefaultKeyFee,
			UndisclosedPetFee = e.UndisclosedPetFee,
			MinimumSmokingFee = e.MinimumSmokingFee,
			UtilityOneBed = e.UtilityOneBed,
			UtilityTwoBed = e.UtilityTwoBed,
			UtilityThreeBed = e.UtilityThreeBed,
			UtilityFourBed = e.UtilityFourBed,
			UtilityHouse = e.UtilityHouse,
			MaidOneBed = e.MaidOneBed,
			MaidTwoBed = e.MaidTwoBed,
			MaidThreeBed = e.MaidThreeBed,
			MaidFourBed = e.MaidFourBed,
			ParkingLowEnd = e.ParkingLowEnd,
			ParkingHighEnd = e.ParkingHighEnd,
			IsActive = e.IsActive
		};
	}
}


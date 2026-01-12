using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.OfficeConfigurations;

public partial class OfficeConfigurationRepository : IOfficeConfigurationRepository
{
	private readonly string _dbConnectionString;

	public OfficeConfigurationRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private OfficeConfiguration ConvertEntityToModel(OfficeConfigurationEntity e)
	{
		return new OfficeConfiguration
		{
			OfficeId = e.OfficeId,
			OfficeCode = e.OfficeCode,
			Name = e.Name,
			MaintenanceEmail = e.MaintenanceEmail,
			AfterHoursPhone = e.AfterHoursPhone,
			AfterHoursInstructions = e.AfterHoursInstructions,
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



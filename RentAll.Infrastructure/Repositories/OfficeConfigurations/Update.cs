using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.OfficeConfigurations;

public partial class OfficeConfigurationRepository : IOfficeConfigurationRepository
{
	public async Task<OfficeConfiguration> UpdateByOfficeIdAsync(OfficeConfiguration officeConfiguration)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<OfficeConfigurationEntity>("dbo.OfficeConfiguration_UpdateByOfficeId", new
		{
			OfficeId = officeConfiguration.OfficeId,
			MaintenanceEmail = officeConfiguration.MaintenanceEmail,
			AfterHoursPhone = officeConfiguration.AfterHoursPhone,
			AfterHoursInstructions = officeConfiguration.AfterHoursInstructions,
			DefaultDeposit = officeConfiguration.DefaultDeposit,
			DefaultSdw = officeConfiguration.DefaultSdw,
			UtilityOneBed = officeConfiguration.UtilityOneBed,
			UtilityTwoBed = officeConfiguration.UtilityTwoBed,
			UtilityThreeBed = officeConfiguration.UtilityThreeBed,
			UtilityFourBed = officeConfiguration.UtilityFourBed,
			UtilityHouse = officeConfiguration.UtilityHouse,
			MaidOneBed = officeConfiguration.MaidOneBed,
			MaidTwoBed = officeConfiguration.MaidTwoBed,
			MaidThreeBed = officeConfiguration.MaidThreeBed,
			MaidFourBed = officeConfiguration.MaidFourBed,
			ParkingLowEnd = officeConfiguration.ParkingLowEnd,
			ParkingHighEnd = officeConfiguration.ParkingHighEnd,
			IsActive = officeConfiguration.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("OfficeConfiguration not found");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}


using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Offices;

public partial class OfficeRepository : IOfficeRepository
{
	public async Task<Office> CreateAsync(Office office)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<OfficeEntity>("Organization.Office_Add", new
		{
			OrganizationId = office.OrganizationId,
			OfficeCode = office.OfficeCode,
			Name = office.Name,
			Address1 = office.Address1,
			Address2 = office.Address2,
			Suite = office.Suite,
			City = office.City,
			State = office.State,
			Zip = office.Zip,
			Phone = office.Phone,
			Fax = office.Fax,
			Website = office.Website,
			LogoPath = office.LogoPath,
			MaintenanceEmail = office.MaintenanceEmail,
			AfterHoursPhone = office.AfterHoursPhone,
			AfterHoursInstructions = office.AfterHoursInstructions,
			DaysToRefundDeposit = office.DaysToRefundDeposit,
			DefaultDeposit = office.DefaultDeposit,
			DefaultSdw = office.DefaultSdw,
			DefaultKeyFee = office.DefaultKeyFee,
			UndisclosedPetFee = office.UndisclosedPetFee,
			MinimumSmokingFee = office.MinimumSmokingFee,
			UtilityOneBed = office.UtilityOneBed,
			UtilityTwoBed = office.UtilityTwoBed,
			UtilityThreeBed = office.UtilityThreeBed,
			UtilityFourBed = office.UtilityFourBed,
			UtilityHouse = office.UtilityHouse,
			MaidOneBed = office.MaidOneBed,
			MaidTwoBed = office.MaidTwoBed,
			MaidThreeBed = office.MaidThreeBed,
			MaidFourBed = office.MaidFourBed,
			ParkingLowEnd = office.ParkingLowEnd,
			ParkingHighEnd = office.ParkingHighEnd,
			IsActive = office.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("Office not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}


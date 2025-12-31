using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository : IOrganizationRepository
{
	public async Task<Organization> CreateAsync(Organization organization)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<OrganizationEntity>("dbo.Organization_Add", new
		{
			OrganizationCode = organization.OrganizationCode,
			Name = organization.Name,
			Address1 = organization.Address1,
			Address2 = organization.Address2,
			Suite = organization.Suite,
			City = organization.City,
			State = organization.State,
			Zip = organization.Zip,
			Phone = organization.Phone,
			Website = organization.Website,
			MaintenanceEmail = organization.MaintenanceEmail,
			AfterHoursPhone = organization.AfterHoursPhone,
			DefaultDeposit = organization.DefaultDeposit,
			UtilityOneBed = organization.UtilityOneBed,
			UtilityTwoBed = organization.UtilityTwoBed,
			UtilityThreeBed = organization.UtilityThreeBed,
			UtilityFourBed = organization.UtilityFourBed,
			UtilityHouse = organization.UtilityHouse,
			MaidOneBed = organization.MaidOneBed,
			MaidTwoBed = organization.MaidTwoBed,
			MaidThreeBed = organization.MaidThreeBed,
			MaidFourBed = organization.MaidFourBed,
			LogoPath = organization.LogoPath,
			IsActive = organization.IsActive,
			CreatedBy = organization.CreatedBy
		});

		if (res == null || !res.Any())
			throw new Exception("Organization not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}





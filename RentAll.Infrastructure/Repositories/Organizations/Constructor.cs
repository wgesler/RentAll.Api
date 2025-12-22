using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository : IOrganizationRepository
{
	private readonly string _dbConnectionString;

	public OrganizationRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}

	private Organization ConvertEntityToModel(OrganizationEntity e)
	{
		return new Organization
		{
			OrganizationId = e.OrganizationId,
			OrganizationCode = e.OrganizationCode,
			Name = e.Name,
			Address1 = e.Address1,
			Address2 = e.Address2,
			Suite = e.Suite,
			City = e.City,
			State = e.State,
			Zip = e.Zip,
			Phone = e.Phone,
			Website = e.Website,
			LogoStorageId = e.LogoStorageId,
			IsActive = e.IsActive,
			CreatedOn = e.CreatedOn,
			CreatedBy = e.CreatedBy,
			ModifiedOn = e.ModifiedOn,
			ModifiedBy = e.ModifiedBy
		};
	}
}





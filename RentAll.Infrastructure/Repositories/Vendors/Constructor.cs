using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Vendors
{
	public partial class VendorRepository : IVendorRepository
	{
		private readonly string _dbConnectionString;

		public VendorRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private Vendor ConvertEntityToModel(VendorEntity e)
		{
			var response = new Vendor()
			{
				VendorId = e.VendorId,
				OrganizationId = e.OrganizationId,
				OfficeId = e.OfficeId,
				OfficeName = e.OfficeName,
				VendorCode = e.VendorCode,
				Name = e.Name,
				Address1 = e.Address1,
				Address2 = e.Address2,
				Suite = e.Suite,
				City = e.City,
				State = e.State,
				Zip = e.Zip,
				Phone = e.Phone,
				Website = e.Website,
				LogoPath = e.LogoPath,
				IsInternational = e.IsInternational,
				IsActive = e.IsActive,
				CreatedOn = e.CreatedOn,
				CreatedBy = e.CreatedBy,
				ModifiedOn = e.ModifiedOn,
				ModifiedBy = e.ModifiedBy
			};

			return response;
		}
	}
}




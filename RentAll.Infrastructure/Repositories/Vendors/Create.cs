using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Vendors
{
	public partial class VendorRepository : IVendorRepository
	{
		public async Task<Vendor> CreateAsync(Vendor vendor)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<VendorEntity>("Organization.Vendor_Add", new
			{
				OrganizationId = vendor.OrganizationId,
				OfficeId = vendor.OfficeId,
				VendorCode = vendor.VendorCode,
				Name = vendor.Name,
				Address1 = vendor.Address1,
				Address2 = vendor.Address2,
				Suite = vendor.Suite,
				City = vendor.City,
				State = vendor.State,
				Zip = vendor.Zip,
				Phone = vendor.Phone,
				Website = vendor.Website,
				LogoPath = vendor.LogoPath,
				IsInternational = vendor.IsInternational,
				CreatedBy = vendor.CreatedBy
			});

			if (res == null || !res.Any())
				throw new Exception("Vendor not created");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}




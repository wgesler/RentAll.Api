using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Vendors
{
	public partial class VendorRepository : IVendorRepository
	{
		public async Task<Vendor> UpdateByIdAsync(Vendor vendor)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<VendorEntity>("dbo.Vendor_UpdateById", new
			{
				OrganizationId = vendor.OrganizationId,
				VendorId = vendor.VendorId,
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
				IsActive = vendor.IsActive,
				ModifiedBy = vendor.ModifiedBy
			});

			if (res == null || !res.Any())
				throw new Exception("Vendor not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}




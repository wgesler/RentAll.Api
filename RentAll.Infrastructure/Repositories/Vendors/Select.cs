using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Vendors
{
	public partial class VendorRepository : IVendorRepository
	{
		public async Task<IEnumerable<Vendor>> GetAllAsync(Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<VendorEntity>("dbo.Vendor_GetAll", new
			{
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<Vendor>();

			return res.Select(ConvertEntityToModel);
		}

		public async Task<Vendor?> GetByIdAsync(Guid vendorId, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<VendorEntity>("dbo.Vendor_GetById", new
			{
				VendorId = vendorId,
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}

		public async Task<Vendor?> GetByVendorCodeAsync(string vendorCode, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<VendorEntity>("dbo.Vendor_GetByCode", new
			{
				VendorCode = vendorCode,
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}

		public async Task<bool> ExistsByVendorCodeAsync(string vendorCode, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var result = await db.DapperProcQueryScalarAsync<int>("dbo.Vendor_ExistsByCode", new
			{
				VendorCode = vendorCode,
				OrganizationId = organizationId
			});

			return result == 1;
		}
	}
}




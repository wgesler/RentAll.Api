using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Vendors
{
	public partial class VendorRepository : IVendorRepository
	{
		public async Task DeleteByIdAsync(Guid vendorId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			await db.DapperProcExecuteAsync("Organization.Vendor_DeleteById", new
			{
				VendorId = vendorId
			});
		}
	}
}




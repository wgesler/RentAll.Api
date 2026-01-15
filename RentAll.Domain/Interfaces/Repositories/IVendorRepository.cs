using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IVendorRepository
{
	// Creates
	Task<Vendor> CreateAsync(Vendor vendor);

	// Selects
	Task<IEnumerable<Vendor>> GetAllAsync(Guid OrganizationId);
	Task<IEnumerable<Vendor>> GetAllByOfficeIdAsync(Guid OrganizationId, string officeAccess);
	Task<Vendor?> GetByIdAsync(Guid vendorId, Guid OrganizationId);
	Task<Vendor?> GetByVendorCodeAsync(string vendorCode, Guid OrganizationId);
	Task<bool> ExistsByVendorCodeAsync(string vendorCode, Guid OrganizationId);

	// Updates
	Task<Vendor> UpdateByIdAsync(Vendor vendor);

	// Deletes
	Task DeleteByIdAsync(Guid vendorId);
}





using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ICompaniesRepository
{
    // Company Creates
    Task<Company> CreateAsync(Company company);

    // Company Selects
    Task<IEnumerable<Company>> GetAllAsync(Guid organizationId);
    Task<IEnumerable<Company>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<Company?> GetByIdAsync(Guid companyId, Guid organizationId);
    Task<Company?> GetByCompanyCodeAsync(string companyCode, Guid organizationId);
    Task<bool> ExistsByCompanyCodeAsync(string companyCode, Guid organizationId);

    // Company Updates
    Task<Company> UpdateByIdAsync(Company company);

    // Company Deletes
    Task DeleteByIdAsync(Guid companyId);

    // Vendor Creates
    Task<Vendor> CreateVendorAsync(Vendor vendor);

    // Vendor Selects
    Task<IEnumerable<Vendor>> GetAllVendorsAsync(Guid organizationId);
    Task<IEnumerable<Vendor>> GetAllVendorsByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<Vendor?> GetVendorByIdAsync(Guid vendorId, Guid organizationId);
    Task<Vendor?> GetByVendorCodeAsync(string vendorCode, Guid organizationId);
    Task<bool> ExistsByVendorCodeAsync(string vendorCode, Guid organizationId);

    // Vendor Updates
    Task<Vendor> UpdateVendorByIdAsync(Vendor vendor);

    // Vendor Deletes
    Task DeleteVendorByIdAsync(Guid vendorId);
}

using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ICompaniesRepository
{
    #region Companies
    Task<IEnumerable<Company>> GetCompaniesByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<Company?> GetCompanyByIdAsync(Guid companyId, Guid organizationId);
    Task<Company?> GetCompanyByCompanyCodeAsync(string companyCode, Guid organizationId);
    Task<bool> ExistsByCompanyCodeAsync(string companyCode, Guid organizationId);

    Task<Company> CreateAsync(Company company);
    Task<Company> UpdateByIdAsync(Company company);
    Task DeleteCompanyByIdAsync(Guid companyId);
    #endregion

    #region Vendors
    Task<IEnumerable<Vendor>> GetVendorsByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<Vendor?> GetVendorByIdAsync(Guid vendorId, Guid organizationId);
    Task<Vendor?> GetVendorByVendorCodeAsync(string vendorCode, Guid organizationId);
    Task<bool> ExistsByVendorCodeAsync(string vendorCode, Guid organizationId);

    Task<Vendor> CreateVendorAsync(Vendor vendor);
    Task<Vendor> UpdateVendorByIdAsync(Vendor vendor);
    Task DeleteVendorByIdAsync(Guid vendorId);
    #endregion
}

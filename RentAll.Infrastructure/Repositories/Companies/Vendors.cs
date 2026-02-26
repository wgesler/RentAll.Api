using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Companies
{
    public partial class CompanyRepository
    {
        #region Create
        public async Task<Vendor> CreateVendorAsync(Vendor vendor)
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
        #endregion

        #region Select
        public async Task<IEnumerable<Vendor>> GetAllVendorsAsync(Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<VendorEntity>("Organization.Vendor_GetAll", new
            {
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Vendor>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<Vendor>> GetAllVendorsByOfficeIdAsync(Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<VendorEntity>("Organization.Vendor_GetAllByOfficeId", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Vendor>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<Vendor?> GetVendorByIdAsync(Guid vendorId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<VendorEntity>("Organization.Vendor_GetById", new
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
            var res = await db.DapperProcQueryAsync<VendorEntity>("Organization.Vendor_GetByCode", new
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
            var result = await db.DapperProcQueryScalarAsync<int>("Organization.Vendor_ExistsByCode", new
            {
                VendorCode = vendorCode,
                OrganizationId = organizationId
            });

            return result == 1;
        }
        #endregion

        #region Update
        public async Task<Vendor> UpdateVendorByIdAsync(Vendor vendor)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<VendorEntity>("Organization.Vendor_UpdateById", new
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
                IsInternational = vendor.IsInternational,
                IsActive = vendor.IsActive,
                ModifiedBy = vendor.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Vendor not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Delete
        public async Task DeleteVendorByIdAsync(Guid vendorId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Organization.Vendor_DeleteById", new
            {
                VendorId = vendorId
            });
        }
        #endregion
    }
}

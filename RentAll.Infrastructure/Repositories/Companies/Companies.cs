using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Companies
{
    public partial class CompanyRepository
    {
        #region Selects
        public async Task<IEnumerable<Company>> GetCompaniesByOfficeIdsAsync(Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<CompanyEntity>("Organization.Company_GetAllByOfficeId", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Company>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<Company?> GetCompanyByIdAsync(Guid companyId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<CompanyEntity>("Organization.Company_GetById", new
            {
                CompanyId = companyId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<Company?> GetCompanyByCompanyCodeAsync(string companyCode, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<CompanyEntity>("Organization.Company_GetByCode", new
            {
                CompanyCode = companyCode,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<bool> ExistsByCompanyCodeAsync(string companyCode, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var result = await db.DapperProcQueryScalarAsync<int>("Organization.Company_ExistsByCode", new
            {
                CompanyCode = companyCode,
                OrganizationId = organizationId
            });

            return result == 1;
        }
        #endregion

        #region Creates
        public async Task<Company> CreateAsync(Company company)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<CompanyEntity>("Organization.Company_Add", new
            {
                OrganizationId = company.OrganizationId,
                OfficeId = company.OfficeId,
                CompanyCode = company.CompanyCode,
                Name = company.Name,
                Address1 = company.Address1,
                Address2 = company.Address2,
                Suite = company.Suite,
                City = company.City,
                State = company.State,
                Zip = company.Zip,
                Phone = company.Phone,
                Website = company.Website,
                LogoPath = company.LogoPath,
                Notes = company.Notes,
                IsInternational = company.IsInternational,
                CreatedBy = company.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Company not created");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Updates
        public async Task<Company> UpdateByIdAsync(Company company)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<CompanyEntity>("Organization.Company_UpdateById", new
            {
                OrganizationId = company.OrganizationId,
                CompanyId = company.CompanyId,
                OfficeId = company.OfficeId,
                CompanyCode = company.CompanyCode,
                Name = company.Name,
                Address1 = company.Address1,
                Address2 = company.Address2,
                Suite = company.Suite,
                City = company.City,
                State = company.State,
                Zip = company.Zip,
                Phone = company.Phone,
                Website = company.Website,
                LogoPath = company.LogoPath,
                Notes = company.Notes,
                IsInternational = company.IsInternational,
                IsActive = company.IsActive,
                ModifiedBy = company.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Company not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Deletes
        public async Task DeleteCompanyByIdAsync(Guid companyId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Organization.Company_DeleteById", new
            {
                CompanyId = companyId
            });
        }
        #endregion
    }
}

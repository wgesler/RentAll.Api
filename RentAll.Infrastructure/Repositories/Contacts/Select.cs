using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Contacts
{
    public partial class ContactRepository : IContactRepository
    {
        public async Task<IEnumerable<Contact>> GetAllAsync(Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("Organization.Contact_GetAll", new
            {
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Contact>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<Contact>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("Organization.Contact_GetAllByOfficeId", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Contact>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<Contact?> GetByIdAsync(Guid contactId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("Organization.Contact_GetById", new
            {
                ContactId = contactId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<Contact?> GetByContactCodeAsync(string contactCode, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("Organization.Contact_GetByCode", new
            {
                ContactCode = contactCode,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<IEnumerable<Contact>> GetByContactTypeIdAsync(int contactTypeId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("Organization.Contact_GetByContactTypeId", new
            {
                ContactTypeId = contactTypeId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Contact>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<bool> ExistsByContactCodeAsync(string contactCode, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var result = await db.DapperProcQueryScalarAsync<int>("Organization.Contact_ExistsByCode", new
            {
                ContactCode = contactCode,
                OrganizationId = organizationId
            });

            return result == 1;
        }
    }
}

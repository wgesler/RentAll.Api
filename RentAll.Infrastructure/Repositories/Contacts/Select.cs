using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Contacts
{
    public partial class ContactRepository : IContactRepository
    {
        public async Task<IEnumerable<Contact>> GetContactsByOfficeIdAsync(Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("Organization.Contact_GetAllByOfficeIds", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Contact>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<Contact>> GetContactsByContactTypeIdAsync(int contactTypeId, Guid organizationId)
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

        public async Task<Contact?> GetContactByIdAsync(Guid contactId, Guid organizationId)
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

        public async Task<Contact?> GetContactByEmailAsync(string email, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("Organization.Contact_GetByEmail", new
            {
                Email = email,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<Contact?> GetContactByContactCodeAsync(string contactCode, Guid organizationId)
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

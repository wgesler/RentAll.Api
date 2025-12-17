using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Contacts;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Contacts
{
    public partial class ContactRepository : IContactRepository
    {
		public async Task<IEnumerable<Contact>> GetAllAsync(Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<ContactEntity>("dbo.Contact_GetAll", new
			{
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<Contact>();

			return res.Select(ConvertEntityToModel);
		}
		
        public async Task<Contact?> GetByIdAsync(Guid contactId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("dbo.Contact_GetById", new
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
            var res = await db.DapperProcQueryAsync<ContactEntity>("dbo.Contact_GetByCode", new
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
            var res = await db.DapperProcQueryAsync<ContactEntity>("dbo.Contact_GetByContactTypeId", new
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
			var result = await db.DapperProcQueryScalarAsync<int>("dbo.Contact_ExistsByCode", new
			{
				ContactCode = contactCode,
				OrganizationId = organizationId
			});

			return result == 1;
		}
	}
}
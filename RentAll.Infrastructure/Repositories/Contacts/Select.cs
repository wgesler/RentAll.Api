using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Contacts;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Contacts
{
    public partial class ContactRepository : IContactRepository
    {
		public async Task<IEnumerable<Contact>> GetAllAsync()
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<ContactEntity>("dbo.Contact_GetAll", null);

			if (res == null || !res.Any())
				return Enumerable.Empty<Contact>();

			return res.Select(ConvertEntityToModel);
		}
		
        public async Task<Contact?> GetByIdAsync(Guid contactId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("dbo.Contact_GetById", new
            {
                ContactId = contactId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<Contact?> GetByContactCodeAsync(string contactCode)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("dbo.Contact_GetByCode", new
            {
                ContactCode = contactCode
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<IEnumerable<Contact>> GetByContactTypeIdAsync(int contactTypeId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("dbo.Contact_GetByContactTypeId", new
            {
                ContactTypeId = contactTypeId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Contact>();

            return res.Select(ConvertEntityToModel);
        }

		public async Task<bool> ExistsByContactCodeAsync(string contactCode)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var result = await db.DapperProcQueryScalarAsync<int>("dbo.Contact_ExistsByCode", new
			{
				ContactCode = contactCode
			});

			return result == 1;
		}
	}
}
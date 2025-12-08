using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Contacts;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Contacts
{
    public partial class ContactRepository : IContactRepository
    {
        public async Task<Contact> CreateAsync(Contact contact)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("dbo.Contact_Add", new
            {
                ContactCode = contact.ContactCode,
                ContactTypeId = contact.ContactTypeId,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Address1 = contact.Address1,
                Address2 = contact.Address2,
                City = contact.City,
                State = contact.State,
                Zip = contact.Zip,
                Phone = contact.Phone,
                Email = contact.Email,
                CreatedBy = contact.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Contact not created");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
    }
}
using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Contacts
{
    public partial class ContactRepository : IContactRepository
    {
        public async Task<Contact> UpdateByIdAsync(Contact contact)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("dbo.Contact_UpdateById", new
            {
                OrganizationId = contact.OrganizationId,
                ContactId = contact.ContactId,
                ContactCode = contact.ContactCode,
                EntityTypeId = (int)contact.EntityType,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Address1 = contact.Address1,
                Address2 = contact.Address2,
                City = contact.City,
                State = contact.State,
                Zip = contact.Zip,
                Phone = contact.Phone,
                Email = contact.Email,
                IsActive = contact.IsActive,
                ModifiedBy = contact.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Contact not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
    }
}
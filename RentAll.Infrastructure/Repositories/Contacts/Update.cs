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
            var res = await db.DapperProcQueryAsync<ContactEntity>("Organization.Contact_UpdateById", new
            {
				ContactId = contact.ContactId,
				OrganizationId = contact.OrganizationId,
				OfficeId = contact.OfficeId,
                ContactCode = contact.ContactCode,
                EntityTypeId = (int)contact.EntityType,
                EntityId = contact.EntityId,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Address1 = contact.Address1,
                Address2 = contact.Address2,
                City = contact.City,
                State = contact.State,
                Zip = contact.Zip,
                Phone = contact.Phone,
                Email = contact.Email,
                Notes = contact.Notes,
                IsInternational = contact.IsInternational,
                IsActive = contact.IsActive,
                ModifiedBy = contact.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Contact not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
    }
}

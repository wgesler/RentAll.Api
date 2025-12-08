using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Contacts;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Contacts
{
    public partial class ContactRepository : IContactRepository
    {
        private readonly string _dbConnectionString;

        public ContactRepository(IOptions<AppSettings> appSettings)
        {
            _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
        }

        private Contact ConvertEntityToModel(ContactEntity e)
        {
            var response = new Contact()
            {
                ContactId = e.ContactId,
                ContactCode = e.ContactCode,
                ContactTypeId = e.ContactTypeId,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Address1 = e.Address1,
                Address2 = e.Address2,
                City = e.City,
                State = e.State,
                Zip = e.Zip,
                Phone = e.Phone,
                Email = e.Email,
                IsActive = e.IsActive,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy,
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy
            };

            return response;
        }
    }
}
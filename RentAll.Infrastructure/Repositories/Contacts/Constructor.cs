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

        private Contact ConvertDtoToModel(ContactEntity dto)
        {
            var response = new Contact()
            {
                ContactId = dto.ContactId,
                ContactCode = dto.ContactCode,
                ContactTypeId = dto.ContactTypeId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                FullName = dto.FullName,
                Address1 = dto.Address1,
                Address2 = dto.Address2,
                City = dto.City,
                State = dto.State,
                Zip = dto.Zip,
                Phone = dto.Phone,
                Email = dto.Email
            };

            return response;
        }
    }
}
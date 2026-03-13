using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;

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
                OrganizationId = e.OrganizationId,
                OfficeId = e.OfficeId,
                OfficeName = e.OfficeName,
                ContactCode = e.ContactCode,
                EntityType = (EntityType)e.EntityTypeId,
                EntityId = e.EntityId,
                CompanyName = e.CompanyName,
                DisplayName = e.DisplayName,
                FirstName = e.FirstName,
                LastName = e.LastName,
                FullName = e.FullName,
                Address1 = e.Address1,
                Address2 = e.Address2,
                City = e.City,
                State = e.State,
                Zip = e.Zip,
                Phone = e.Phone,
                Email = e.Email,
                Rating = e.Rating,
                Notes = e.Notes,
                IsInternational = e.IsInternational,
                W9Path = e.W9Path,
                W9Expiration = e.W9Expiration,
                InsurancePath = e.InsurancePath,
                InsuranceExpiration = e.InsuranceExpiration,
                Markup = e.Markup,
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

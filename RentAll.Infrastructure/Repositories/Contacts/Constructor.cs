using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using System.Text.Json;

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
            var properties = new List<string>();
            if (!string.IsNullOrWhiteSpace(e.Properties))
            {
                try
                {
                    properties = JsonSerializer.Deserialize<List<string>>(e.Properties) ?? new List<string>();
                }
                catch
                {
                    properties = new List<string>();
                }
            }

            List<int> officeAccess = new List<int>();
            if (!string.IsNullOrWhiteSpace(e.OfficeAccess))
            {
                try
                {
                    officeAccess = JsonSerializer.Deserialize<List<int>>(e.OfficeAccess) ?? new List<int>();
                }
                catch
                {
                    officeAccess = new List<int>();
                }
            }

            var response = new Contact()
            {
                ContactId = e.ContactId,
                UserId = e.UserId,
                OrganizationId = e.OrganizationId,
                OfficeId = e.OfficeId,
                OfficeName = e.OfficeName,
                OfficeAccess = officeAccess,
                ContactCode = e.ContactCode,
                EntityType = (EntityType)e.EntityTypeId,
                OwnerType = (OwnerType?)e.OwnerTypeId,
                Properties = properties,
                CompanyName = e.CompanyName,
                CompanyEmail = e.CompanyEmail,
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
                InsurancePath = e.InsurancePath,
                InsuranceExpiration = e.InsuranceExpiration,
                Markup = e.Markup,
                RevenueSplitOwner = e.RevenueSplitOwner,
                RevenueSplitOffice = e.RevenueSplitOffice,
                WorkingCapitalBalance = e.WorkingCapitalBalance,
                LinenAndTowelFee = e.LinenAndTowelFee,
                BankName = e.BankName,
                RoutingNumber = e.RoutingNumber,
                AccountNumber = e.AccountNumber,
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

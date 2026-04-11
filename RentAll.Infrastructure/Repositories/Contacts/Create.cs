using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Contacts
{
    public partial class ContactRepository : IContactRepository
    {
        public async Task<Contact> CreateAsync(Contact contact)
        {
            var propertiesJson = contact.Properties != null && contact.Properties.Any()
                ? JsonSerializer.Serialize(contact.Properties)
                : "[]";

            var officeAccessJson = contact.OfficeAccess != null && contact.OfficeAccess.Any()
                ? JsonSerializer.Serialize(contact.OfficeAccess)
                : "[]";

            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ContactEntity>("Organization.Contact_Add", new
            {
                OrganizationId = contact.OrganizationId,
                UserId = contact.UserId,
                OfficeId = contact.OfficeId,
                ContactCode = contact.ContactCode,
                EntityTypeId = (int)contact.EntityType,
                OwnerTypeId = (int?)contact.OwnerType,
                VendorTypeId = (int?)contact.VendorType,
                Properties = propertiesJson,
                CompanyName = contact.CompanyName,
                CompanyEmail = contact.CompanyEmail,
                DisplayName = contact.DisplayName,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                OfficeAccess = officeAccessJson,
                Address1 = contact.Address1,
                Address2 = contact.Address2,
                City = contact.City,
                State = contact.State,
                Zip = contact.Zip,
                Phone = contact.Phone,
                Email = contact.Email,
                Rating = contact.Rating,
                Notes = contact.Notes,
                IsInternational = contact.IsInternational,
                W9Path = contact.W9Path,
                InsurancePath = contact.InsurancePath,
                InsuranceExpiration = contact.InsuranceExpiration,
                Markup = contact.Markup,
                RevenueSplitOwner = contact.RevenueSplitOwner,
                RevenueSplitOffice = contact.RevenueSplitOffice,
                WorkingCapitalBalance = contact.WorkingCapitalBalance,
                LinenAndTowelFee = contact.LinenAndTowelFee,
                BankName = contact.BankName,
                RoutingNumber = contact.RoutingNumber,
                AccountNumber = contact.AccountNumber,
                CreatedBy = contact.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Contact not created");



            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
    }
}

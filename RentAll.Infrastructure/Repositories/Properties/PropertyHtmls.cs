using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository
    {
        #region Create
        public async Task<PropertyHtml> CreatePropertyHtmlAsync(PropertyHtml propertyHtml)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyHtmlEntity>("Property.PropertyHtml_UpsertByPropertyId", new
            {
                PropertyId = propertyHtml.PropertyId,
                OrganizationId = propertyHtml.OrganizationId,
                WelcomeLetter = propertyHtml.WelcomeLetter,
                InspectionChecklist = propertyHtml.InspectionChecklist,
                Lease = propertyHtml.Lease,
                Invoice = propertyHtml.Invoice,
                LetterOfResponsibility = propertyHtml.LetterOfResponsibility,
                NoticeToVacate = propertyHtml.NoticeToVacate,
                CreditAuthorization = propertyHtml.CreditAuthorization,
                CreditApplicationBusiness = propertyHtml.CreditApplicationBusiness,
                CreditApplicationIndividual = propertyHtml.CreditApplicationIndividual,
                CreatedBy = propertyHtml.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("PropertyHtml not created");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Select
        public async Task<PropertyHtml?> GetPropertyHtmlByPropertyIdAsync(Guid propertyId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyHtmlEntity>("Property.PropertyHtml_GetByPropertyId", new
            {
                PropertyId = propertyId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Update
        public async Task<PropertyHtml> UpdatePropertyHtmlByIdAsync(PropertyHtml propertyHtml)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyHtmlEntity>("Property.PropertyHtml_UpsertByPropertyId", new
            {
                PropertyId = propertyHtml.PropertyId,
                OrganizationId = propertyHtml.OrganizationId,
                WelcomeLetter = propertyHtml.WelcomeLetter,
                InspectionChecklist = propertyHtml.InspectionChecklist,
                Lease = propertyHtml.Lease,
                Invoice = propertyHtml.Invoice,
                LetterOfResponsibility = propertyHtml.LetterOfResponsibility,
                NoticeToVacate = propertyHtml.NoticeToVacate,
                CreditAuthorization = propertyHtml.CreditAuthorization,
                CreditApplicationBusiness = propertyHtml.CreditApplicationBusiness,
                CreditApplicationIndividual = propertyHtml.CreditApplicationIndividual,
                ModifiedBy = propertyHtml.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("PropertyHtml not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Delete
        public async Task DeletePropertyHtmlByPropertyIdAsync(Guid propertyId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Property.PropertyHtml_DeleteByPropertyId", new
            {
                PropertyId = propertyId
            });
        }
        #endregion
    }
}

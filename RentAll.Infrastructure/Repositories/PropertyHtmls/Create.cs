using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.PropertyHtmls
{
	public partial class PropertyHtmlRepository : IPropertyHtmlRepository
	{
		public async Task<PropertyHtml> CreateAsync(PropertyHtml propertyHtml)
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
	}
}


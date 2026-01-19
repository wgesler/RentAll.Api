using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.PropertyHtmls
{
	public partial class PropertyHtmlRepository : IPropertyHtmlRepository
	{
		public async Task<PropertyHtml> UpdateByIdAsync(PropertyHtml propertyHtml)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<PropertyHtmlEntity>("dbo.PropertyHtml_UpsertByPropertyId", new
			{
				PropertyId = propertyHtml.PropertyId,
				OrganizationId = propertyHtml.OrganizationId,
				WelcomeLetter = propertyHtml.WelcomeLetter,
				Lease = propertyHtml.Lease,
				LetterOfResponsibility = propertyHtml.LetterOfResponsibility,
				NoticeToVacate = propertyHtml.NoticeToVacate,
				CreditAuthorization = propertyHtml.CreditAuthorization,
				CreditApplication = propertyHtml.CreditApplication,
				ModifiedBy = propertyHtml.ModifiedBy
			});

			if (res == null || !res.Any())
				throw new Exception("PropertyHtml not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}


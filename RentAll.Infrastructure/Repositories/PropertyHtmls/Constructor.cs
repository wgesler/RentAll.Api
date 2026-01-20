using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.PropertyHtmls
{
	public partial class PropertyHtmlRepository : IPropertyHtmlRepository
	{
		private readonly string _dbConnectionString;

		public PropertyHtmlRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private PropertyHtml ConvertEntityToModel(PropertyHtmlEntity e)
		{
			var response = new PropertyHtml()
			{
				PropertyId = e.PropertyId,
				OrganizationId = e.OrganizationId,
				WelcomeLetter = e.WelcomeLetter,
				InspectionChecklist = e.InspectionChecklist,
				Lease = e.Lease,
				LetterOfResponsibility = e.LetterOfResponsibility,
				NoticeToVacate = e.NoticeToVacate,
				CreditAuthorization = e.CreditAuthorization,
				CreditApplicationBusiness = e.CreditApplicationBusiness,
				CreditApplicationIndividual = e.CreditApplicationIndividual,
				IsDeleted = e.IsDeleted,
				CreatedOn = e.CreatedOn,
				CreatedBy = e.CreatedBy,
				ModifiedOn = e.ModifiedOn,
				ModifiedBy = e.ModifiedBy
			};

			return response;
		}
	}
}


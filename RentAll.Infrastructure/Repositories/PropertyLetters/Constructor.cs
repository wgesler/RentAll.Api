using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Entities;
using RentAll.Domain.Models;

namespace RentAll.Infrastructure.Repositories.PropertyLetters
{
	public partial class PropertyLetterRepository : IPropertyLetterRepository
	{
		private readonly string _dbConnectionString;

		public PropertyLetterRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private PropertyLetter ConvertEntityToModel(PropertyLetterEntity e)
		{
			var response = new PropertyLetter()
			{
				PropertyId = e.PropertyId,
				OrganizationId = e.OrganizationId,
				ArrivalInstructions = e.ArrivalInstructions,
				MailboxInstructions = e.MailboxInstructions,
				PackageInstructions = e.PackageInstructions,
				ParkingInformation = e.ParkingInformation,
				Access = e.Access,
				Amenities = e.Amenities,
				Laundry = e.Laundry,
				ProvidedFurnishings = e.ProvidedFurnishings,
				Housekeeping = e.Housekeeping,
				TelevisionSource = e.TelevisionSource,
				InternetService = e.InternetService,
				KeyReturn = e.KeyReturn,
				Concierge = e.Concierge,
				MaintenanceEmail = e.MaintenanceEmail,
				EmergencyPhone = e.EmergencyPhone,
				AdditionalNotes = e.AdditionalNotes,
				CreatedOn = e.CreatedOn,
				CreatedBy = e.CreatedBy,
				ModifiedOn = e.ModifiedOn,
				ModifiedBy = e.ModifiedBy
			};

			return response;
		}
	}
}


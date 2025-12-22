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
				ArrivalInstructions = e.ArrivalInstructions,
				MailboxInstructions = e.MailboxInstructions,
				PackageInstructions = e.PackageInstructions,
				ParkingInformation = e.ParkingInformation,
				Amenities = e.Amenities,
				Laundry = e.Laundry,
				ProvidedFurnishings = e.ProvidedFurnishings,
				Housekeeping = e.Housekeeping,
				TelevisionSource = e.TelevisionSource,
				InternetService = e.InternetService,
				InternetNetwork = e.InternetNetwork,
				InternetPassword = e.InternetPassword,
				KeyReturn = e.KeyReturn,
				Concierge = e.Concierge,
				GuestServiceEmail = e.GuestServiceEmail,
				CreatedOn = e.CreatedOn,
				CreatedBy = e.CreatedBy,
				ModifiedOn = e.ModifiedOn,
				ModifiedBy = e.ModifiedBy
			};

			return response;
		}
	}
}


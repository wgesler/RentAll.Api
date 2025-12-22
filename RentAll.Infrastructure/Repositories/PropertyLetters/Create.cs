using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.PropertyLetters
{
	public partial class PropertyLetterRepository : IPropertyLetterRepository
	{
		public async Task<PropertyLetter> CreateAsync(PropertyLetter propertyLetter)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<PropertyLetterEntity>("dbo.PropertyLetter_Add", new
			{
				PropertyId = propertyLetter.PropertyId,
				ArrivalInstructions = propertyLetter.ArrivalInstructions,
				MailboxInstructions = propertyLetter.MailboxInstructions,
				PackageInstructions = propertyLetter.PackageInstructions,
				ParkingInformation = propertyLetter.ParkingInformation,
				Amenities = propertyLetter.Amenities,
				Laundry = propertyLetter.Laundry,
				ProvidedFurnishings = propertyLetter.ProvidedFurnishings,
				Housekeeping = propertyLetter.Housekeeping,
				TelevisionSource = propertyLetter.TelevisionSource,
				InternetService = propertyLetter.InternetService,
				InternetNetwork = propertyLetter.InternetNetwork,
				InternetPassword = propertyLetter.InternetPassword,
				KeyReturn = propertyLetter.KeyReturn,
				Concierge = propertyLetter.Concierge,
				GuestServiceEmail = propertyLetter.GuestServiceEmail,
				CreatedBy = propertyLetter.CreatedBy
			});

			if (res == null || !res.Any())
				throw new Exception("PropertyLetter not created");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}


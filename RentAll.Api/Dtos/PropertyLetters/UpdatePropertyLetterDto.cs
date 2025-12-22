using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.PropertyLetters;

public class UpdatePropertyLetterDto
{
	public Guid PropertyId { get; set; }
	public string? ArrivalInstructions { get; set; }
	public string? MailboxInstructions { get; set; }
	public string? PackageInstructions { get; set; }
	public string? ParkingInformation { get; set; }
	public string? Amenities { get; set; }
	public string? Laundry { get; set; }
	public string? ProvidedFurnishings { get; set; }
	public string? Housekeeping { get; set; }
	public string? TelevisionSource { get; set; }
	public string? InternetService { get; set; }
	public string? InternetNetwork { get; set; }
	public string? InternetPassword { get; set; }
	public string? KeyReturn { get; set; }
	public string? Concierge { get; set; }
	public string? GuestServiceEmail { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (PropertyId == Guid.Empty)
			return (false, "PropertyId is required");

		return (true, null);
	}

	public PropertyLetter ToModel(Guid currentUser)
	{
		return new PropertyLetter
		{
			PropertyId = PropertyId,
			ArrivalInstructions = ArrivalInstructions,
			MailboxInstructions = MailboxInstructions,
			PackageInstructions = PackageInstructions,
			ParkingInformation = ParkingInformation,
			Amenities = Amenities,
			Laundry = Laundry,
			ProvidedFurnishings = ProvidedFurnishings,
			Housekeeping = Housekeeping,
			TelevisionSource = TelevisionSource,
			InternetService = InternetService,
			InternetNetwork = InternetNetwork,
			InternetPassword = InternetPassword,
			KeyReturn = KeyReturn,
			Concierge = Concierge,
			GuestServiceEmail = GuestServiceEmail,
			ModifiedBy = currentUser
		};
	}
}


using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.PropertyLetters;

public class CreatePropertyLetterDto
{
	public Guid PropertyId { get; set; }
	public Guid OrganizationId { get; set; }
	public string? ArrivalInstructions { get; set; }
	public string? MailboxInstructions { get; set; }
	public string? PackageInstructions { get; set; }
	public string? ParkingInformation { get; set; }
	public string? Access { get; set; }
	public string? Amenities { get; set; }
	public string? Laundry { get; set; }
	public string? ProvidedFurnishings { get; set; }
	public string? Housekeeping { get; set; }
	public string? TelevisionSource { get; set; }
	public string? InternetService { get; set; }
	public string? KeyReturn { get; set; }
	public string? Concierge { get; set; }
	public string? MaintenanceEmail { get; set; }
	public string? EmergencyPhone { get; set; }
	public string? AdditionalNotes { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (PropertyId == Guid.Empty)
			return (false, "PropertyId is required");

		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		return (true, null);
	}

	public PropertyLetter ToModel(Guid currentUser)
	{
		return new PropertyLetter
		{
			PropertyId = PropertyId,
			OrganizationId = OrganizationId,
			ArrivalInstructions = ArrivalInstructions,
			MailboxInstructions = MailboxInstructions,
			PackageInstructions = PackageInstructions,
			ParkingInformation = ParkingInformation,
			Access = Access,
			Amenities = Amenities,
			Laundry = Laundry,
			ProvidedFurnishings = ProvidedFurnishings,
			Housekeeping = Housekeeping,
			TelevisionSource = TelevisionSource,
			InternetService = InternetService,
			KeyReturn = KeyReturn,
			Concierge = Concierge,
			MaintenanceEmail = MaintenanceEmail,
			EmergencyPhone = EmergencyPhone,
			AdditionalNotes = AdditionalNotes,
			CreatedBy = currentUser
		};
	}
}


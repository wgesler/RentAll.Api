using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.PropertyLetters;

public class PropertyLetterResponseDto
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

	public PropertyLetterResponseDto(PropertyLetter propertyLetter)
	{
		PropertyId = propertyLetter.PropertyId;
		ArrivalInstructions = propertyLetter.ArrivalInstructions;
		MailboxInstructions = propertyLetter.MailboxInstructions;
		PackageInstructions = propertyLetter.PackageInstructions;
		ParkingInformation = propertyLetter.ParkingInformation;
		Amenities = propertyLetter.Amenities;
		Laundry = propertyLetter.Laundry;
		ProvidedFurnishings = propertyLetter.ProvidedFurnishings;
		Housekeeping = propertyLetter.Housekeeping;
		TelevisionSource = propertyLetter.TelevisionSource;
		InternetService = propertyLetter.InternetService;
		InternetNetwork = propertyLetter.InternetNetwork;
		InternetPassword = propertyLetter.InternetPassword;
		KeyReturn = propertyLetter.KeyReturn;
		Concierge = propertyLetter.Concierge;
		GuestServiceEmail = propertyLetter.GuestServiceEmail;
	}
}


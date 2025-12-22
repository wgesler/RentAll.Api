namespace RentAll.Domain.Models;

public class PropertyLetter
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
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }
}


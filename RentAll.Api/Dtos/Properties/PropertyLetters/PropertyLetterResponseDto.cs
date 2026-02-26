namespace RentAll.Api.Dtos.Properties.PropertyLetters;

public class PropertyLetterResponseDto
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

    public PropertyLetterResponseDto(PropertyLetter propertyLetter)
    {
        PropertyId = propertyLetter.PropertyId;
        OrganizationId = propertyLetter.OrganizationId;
        ArrivalInstructions = propertyLetter.ArrivalInstructions;
        MailboxInstructions = propertyLetter.MailboxInstructions;
        PackageInstructions = propertyLetter.PackageInstructions;
        ParkingInformation = propertyLetter.ParkingInformation;
        Access = propertyLetter.Access;
        Amenities = propertyLetter.Amenities;
        Laundry = propertyLetter.Laundry;
        ProvidedFurnishings = propertyLetter.ProvidedFurnishings;
        Housekeeping = propertyLetter.Housekeeping;
        TelevisionSource = propertyLetter.TelevisionSource;
        InternetService = propertyLetter.InternetService;
        KeyReturn = propertyLetter.KeyReturn;
        Concierge = propertyLetter.Concierge;
        MaintenanceEmail = propertyLetter.MaintenanceEmail;
        EmergencyPhone = propertyLetter.EmergencyPhone;
        AdditionalNotes = propertyLetter.AdditionalNotes;
    }
}


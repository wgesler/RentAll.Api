namespace RentAll.Api.Dtos.Properties.PropertyInformations;

public class PropertyInformationResponseDto
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

    public PropertyInformationResponseDto(PropertyInformation propertyInformation)
    {
        PropertyId = propertyInformation.PropertyId;
        OrganizationId = propertyInformation.OrganizationId;
        ArrivalInstructions = propertyInformation.ArrivalInstructions;
        MailboxInstructions = propertyInformation.MailboxInstructions;
        PackageInstructions = propertyInformation.PackageInstructions;
        ParkingInformation = propertyInformation.ParkingInformation;
        Access = propertyInformation.Access;
        Amenities = propertyInformation.Amenities;
        Laundry = propertyInformation.Laundry;
        ProvidedFurnishings = propertyInformation.ProvidedFurnishings;
        Housekeeping = propertyInformation.Housekeeping;
        TelevisionSource = propertyInformation.TelevisionSource;
        InternetService = propertyInformation.InternetService;
        KeyReturn = propertyInformation.KeyReturn;
        Concierge = propertyInformation.Concierge;
        MaintenanceEmail = propertyInformation.MaintenanceEmail;
        EmergencyPhone = propertyInformation.EmergencyPhone;
        AdditionalNotes = propertyInformation.AdditionalNotes;
    }
}


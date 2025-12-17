using RentAll.Domain.Models.Contacts;

namespace RentAll.Api.Dtos.Contacts;

public class ContactResponseDto
{
    public Guid ContactId { get; set; }
    public Guid OrganizationId { get; set; }
    public string ContactCode { get; set; } = string.Empty;
    public int EntityTypeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ContactResponseDto(Contact contact)
    {
        ContactId = contact.ContactId;
        OrganizationId = contact.OrganizationId;
        ContactCode = contact.ContactCode;
        EntityTypeId = (int)contact.EntityType;
        FirstName = contact.FirstName;
        LastName = contact.LastName;
        Address1 = contact.Address1;
        Address2 = contact.Address2;
        City = contact.City;
        State = contact.State;
        Zip = contact.Zip;
        Phone = contact.Phone;
        Email = contact.Email;
        IsActive = contact.IsActive;
    }
}

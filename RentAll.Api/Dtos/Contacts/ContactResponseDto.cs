using RentAll.Domain.Models.Contacts;

namespace RentAll.Api.Dtos.Contacts;

public class ContactResponseDto
{
    public Guid ContactId { get; set; }
    public string ContactCode { get; set; } = string.Empty;
    public int ContactTypeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public ContactResponseDto(Contact contact)
    {
        ContactId = contact.ContactId;
        ContactCode = contact.ContactCode;
        ContactTypeId = contact.ContactTypeId;
        FirstName = contact.FirstName;
        LastName = contact.LastName;
        FullName = contact.FullName;
        Address1 = contact.Address1;
        Address2 = contact.Address2;
        City = contact.City;
        State = contact.State;
        Zip = contact.Zip;
        Phone = contact.Phone;
        Email = contact.Email;
    }
}
using RentAll.Domain.Models.Contacts;

namespace RentAll.Api.Dtos.Contacts;

public class CreateContactDto
{
    public string ContactCode { get; set; } = string.Empty;
    public int ContactTypeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(ContactCode))
            return (false, "Contact Code is required");

        if (string.IsNullOrWhiteSpace(FirstName))
            return (false, "First Name is required");

        if (string.IsNullOrWhiteSpace(LastName))
            return (false, "Last Name is required");

        if (string.IsNullOrWhiteSpace(Phone))
            return (false, "Phone is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        return (true, null);
    }

    public Contact ToModel()
    {
        var fullName = $"{FirstName} {LastName}".Trim();
        return new Contact
        {
            ContactId = Guid.NewGuid(),
            ContactCode = ContactCode,
            ContactTypeId = ContactTypeId,
            FirstName = FirstName,
            LastName = LastName,
            FullName = fullName,
            Address1 = Address1,
            Address2 = Address2,
            City = City,
            State = State,
            Zip = Zip,
            Phone = Phone,
            Email = Email
        };
    }
}
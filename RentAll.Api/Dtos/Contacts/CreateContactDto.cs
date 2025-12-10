using RentAll.Domain.Models.Contacts;
using RentAll.Domain.Enums;

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
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(ContactCode))
            return (false, "Contact Code is required");

        if (ContactTypeId <= 0)
            return (false, "Contact Type ID is required");

        if (string.IsNullOrWhiteSpace(FirstName))
            return (false, "First Name is required");

        if (string.IsNullOrWhiteSpace(LastName))
            return (false, "Last Name is required");

        if (string.IsNullOrWhiteSpace(Phone))
            return (false, "Phone is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        // Validate enum value
        if (!Enum.IsDefined(typeof(ContactType), ContactTypeId))
            return (false, $"Invalid ContactType value: {ContactTypeId}");

        return (true, null);
    }

    public Contact ToModel(CreateContactDto c, Guid currentUser)
    {
        return new Contact
        {
            ContactCode = c.ContactCode,
            ContactTypeId = (ContactType)c.ContactTypeId,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Address1 = c.Address1,
            Address2 = c.Address2,
            City = c.City,
            State = c.State,
            Zip = c.Zip,
            Phone = c.Phone,
            Email = c.Email,
            IsActive = true,
            CreatedBy = currentUser
        };
    }
}

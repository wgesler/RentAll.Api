using RentAll.Domain.Models.Contacts;
using RentAll.Domain.Enums;

namespace RentAll.Api.Dtos.Contacts;

public class CreateContactDto
{
	public Guid OrganizationId { get; set; }
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

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (EntityTypeId <= 0)
            return (false, "Entity Type ID is required");

        if (string.IsNullOrWhiteSpace(FirstName))
            return (false, "First Name is required");

        if (string.IsNullOrWhiteSpace(LastName))
            return (false, "Last Name is required");

        if (string.IsNullOrWhiteSpace(Phone))
            return (false, "Phone is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        // Validate enum value
        if (!Enum.IsDefined(typeof(EntityType), EntityTypeId))
            return (false, $"Invalid EntityType value: {EntityTypeId}");

        return (true, null);
    }

    public Contact ToModel(CreateContactDto c, string code, Guid currentUser)
    {
        return new Contact
        {
            OrganizationId = c.OrganizationId,
            ContactCode = code,
            EntityType = (EntityType)c.EntityTypeId,
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

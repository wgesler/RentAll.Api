using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Contacts;

public class CreateContactDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int EntityTypeId { get; set; }
    public Guid? EntityId { get; set; }
    public int? OwnerTypeId { get; set; }
    public string? CompanyName { get; set; }
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Phone { get; set; }
    public string Email { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public bool IsInternational { get; set; }
    public FileDetails? W9FileDetails { get; set; }
    public DateTimeOffset? W9Expiration { get; set; }
    public FileDetails? InsuranceFileDetails { get; set; }
    public DateTimeOffset? InsuranceExpiration { get; set; }
    public int Markup { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (EntityTypeId <= 0)
            return (false, "Entity Type ID is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        // Validate enum values
        if (!Enum.IsDefined(typeof(EntityType), EntityTypeId))
            return (false, $"Invalid EntityType value: {EntityTypeId}");

        if (OwnerTypeId.HasValue && !Enum.IsDefined(typeof(OwnerType), OwnerTypeId.Value))
            return (false, $"Invalid OwnerType value: {OwnerTypeId}");

        return (true, null);
    }

    public Contact ToModel(string code, Guid currentUser)
    {
        return new Contact
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            ContactCode = code,
            EntityType = (EntityType)EntityTypeId,
            EntityId = EntityId,
            OwnerType = (OwnerType?)OwnerTypeId,
            CompanyName = CompanyName,
            DisplayName = DisplayName,
            FirstName = FirstName,
            LastName = LastName,
            Address1 = Address1,
            Address2 = Address2,
            City = City,
            State = State,
            Zip = Zip,
            Phone = Phone,
            Email = Email,
            Rating = Rating,
            Notes = Notes,
            IsInternational = IsInternational,
            W9Path = null, // Will be set by controller after file save
            W9Expiration = W9Expiration,
            InsurancePath = null,// Will be set by controller after file save
            InsuranceExpiration = InsuranceExpiration,
            Markup = Markup,
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }
}

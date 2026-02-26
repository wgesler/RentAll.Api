namespace RentAll.Api.Dtos.Contacts;

public class ContactResponseDto
{
    public Guid ContactId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string ContactCode { get; set; } = string.Empty;
    public int EntityTypeId { get; set; }
    public Guid? EntityId { get; set; }
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
    public string? Notes { get; set; }
    public bool IsInternational { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }


    public ContactResponseDto(Contact contact)
    {
        ContactId = contact.ContactId;
        OrganizationId = contact.OrganizationId;
        OfficeId = contact.OfficeId;
        OfficeName = contact.OfficeName;
        ContactCode = contact.ContactCode;
        EntityTypeId = (int)contact.EntityType;
        EntityId = contact.EntityId;
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
        Notes = contact.Notes;
        IsInternational = contact.IsInternational;
        IsActive = contact.IsActive;
        CreatedOn = contact.CreatedOn;
        CreatedBy = contact.CreatedBy;
        ModifiedOn = contact.ModifiedOn;
        ModifiedBy = contact.ModifiedBy;
    }
}

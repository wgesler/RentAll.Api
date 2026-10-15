using RentAll.Api.Dtos.Properties.Properties;

namespace RentAll.Api.Dtos.Contacts;

public class ExternalContactResponseDto
{
    public string? ExternalRefNo { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Extension { get; set; }
    public string? PreferredName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsInternational { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public int? FederalTaxClassificationId { get; set; }
    public int? OwnerIdTypeId { get; set; }
    public string? IdentificationNumber { get; set; }
    public int? OwnerDocumentId { get; set; }
    public int? PropertyManagedBy { get; set; }
    public ExternalContactImageDto? DocumentFront { get; set; }
    public ExternalContactImageDto? DocumentBack { get; set; }
    public int EntityTypeId { get; set; }
    public int? OwnerTypeId { get; set; }
    public int? VendorTypeId { get; set; }
    public string? CompanyName { get; set; }
    public int? CardTypeId { get; set; }
    public string? CardName { get; set; }
    public string? CardNumber { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    public List<string> Properties { get; set; } = [];

    public static ExternalContactResponseDto FromContact(Contact contact)
    {
        return new ExternalContactResponseDto
        {
            FirstName = contact.FirstName ?? string.Empty,
            LastName = contact.LastName ?? string.Empty,
            Email = contact.Email ?? string.Empty,
            ExternalRefNo = string.IsNullOrWhiteSpace(contact.ExternalRefNo) ? null : contact.ExternalRefNo.Trim(),
            Phone = contact.Phone,
            Extension = contact.Extension,
            PreferredName = contact.PreferredName,
            Notes = contact.Notes,
            IsActive = contact.IsActive,
            IsInternational = contact.IsInternational,
            Address1 = contact.Address1,
            Address2 = contact.Address2,
            City = contact.City,
            State = contact.State,
            Zip = contact.Zip,
            DateOfBirth = contact.DateOfBirth,
            FederalTaxClassificationId = contact.FederalTaxClassification.HasValue ? (int)contact.FederalTaxClassification.Value : null,
            OwnerIdTypeId = contact.OwnerIdType.HasValue ? (int)contact.OwnerIdType.Value : null,
            IdentificationNumber = contact.IdentificationNumber,
            OwnerDocumentId = contact.OwnerDocument.HasValue ? (int)contact.OwnerDocument.Value : null,
            PropertyManagedBy = contact.PropertyManagedBy.HasValue ? (int)contact.PropertyManagedBy.Value : null,
            EntityTypeId = (int)contact.EntityType,
            OwnerTypeId = (int)contact.OwnerType,
            VendorTypeId = (int)contact.VendorType,
            CompanyName = contact.CompanyName,
            BankName = contact.BankName,
            RoutingNumber = contact.RoutingNumber,
            Properties = (contact.Properties ?? []).Where(code => !string.IsNullOrWhiteSpace(code)).Select(code => code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    public static bool IsExportable(Contact contact) =>
        contact.IsActive && ExternalPropertyContactDto.IsAllowedContactEntityType((int)contact.EntityType);
}

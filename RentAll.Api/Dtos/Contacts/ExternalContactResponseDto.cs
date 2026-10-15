namespace RentAll.Api.Dtos.Contacts;

public class ExternalContactResponseDto
{
    public string? ContactCode { get; set; }
    public string? ExternalRefNo { get; set; }
    public int? OwnerTypeId { get; set; }
    public string? CompanyName { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Phone { get; set; }
    public string? Extension { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsInternational { get; set; }
    public int? PropertyManagedBy { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public int? FederalTaxClassificationId { get; set; }
    public int? OwnerIdTypeId { get; set; }
    public string? IdentificationNumber { get; set; }
    public int? OwnerDocumentId { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    public string? AccountNumber { get; set; }
    public int? CardTypeId { get; set; }
    public string? CardName { get; set; }
    public string? CardNumber { get; set; }
    public bool IsActive { get; set; }
    public List<string> Properties { get; set; } = [];

    public static ExternalContactResponseDto FromContact(Contact contact)
    {
        return new ExternalContactResponseDto
        {
            ContactCode = string.IsNullOrWhiteSpace(contact.ContactCode) ? null : contact.ContactCode.Trim(),
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
            OwnerTypeId = (int)contact.OwnerType,
            CompanyName = contact.CompanyName,
            BankName = contact.BankName,
            RoutingNumber = contact.RoutingNumber,
            AccountNumber = MaskAccountNumber(contact.AccountNumberLastFour),
            Properties = (contact.Properties ?? []).Where(code => !string.IsNullOrWhiteSpace(code)).Select(code => code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    public void ApplyCard(int cardTypeId, string? cardName, string? lastFour)
    {
        CardTypeId = cardTypeId;
        CardName = string.IsNullOrWhiteSpace(cardName) ? null : cardName.Trim();
        CardNumber = MaskCardNumber(lastFour);
    }

    public static string? MaskCardNumber(string? lastFour)
    {
        var digits = new string((lastFour ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
            return null;

        var suffix = digits[^4..];
        return $"**** **** **** {suffix}";
    }

    public static string? MaskAccountNumber(string? lastFour)
    {
        var digits = new string((lastFour ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
            return null;

        return $"{new string('*', 8)}{digits[^4..]}";
    }

    public static bool IsExportable(Contact contact) =>
        contact.IsActive && contact.EntityType == EntityType.Owner;
}

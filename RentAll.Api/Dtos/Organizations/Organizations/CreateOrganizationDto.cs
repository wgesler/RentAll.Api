using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Organizations.Organizations;

public class CreateOrganizationDto
{
    public string Name { get; set; } = string.Empty;
    public int OrganizationTypeId { get; set; }
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Fax { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? Website { get; set; }
    public string Domain { get; set; } = string.Empty;
    public FileDetails? FileDetails { get; set; }
    public bool IsInternational { get; set; }
    public int CurrentInvoiceNo { get; set; }
    public string? SendGridName { get; set; }
    public string? SuffixKeyName { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (!Enum.IsDefined(typeof(OrganizationType), OrganizationTypeId))
            return (false, $"Invalid OrganizationTypeId value: {OrganizationTypeId}");

        if (string.IsNullOrWhiteSpace(Address1))
            return (false, "Address1 is required");

        if (string.IsNullOrWhiteSpace(Phone))
            return (false, "Phone is required");

        if (string.IsNullOrWhiteSpace(Domain))
            return (false, "Domain is required");

        return (true, null);
    }

    public Organization ToModel(string code, Guid currentUser)
    {
        return new Organization
        {
            OrganizationCode = code,
            Name = Name,
            OrganizationType = (OrganizationType)OrganizationTypeId,
            Address1 = Address1,
            Address2 = Address2,
            Suite = Suite,
            City = City,
            State = State,
            Zip = Zip,
            Phone = Phone,
            Fax = Fax,
            ContactName = ContactName,
            ContactEmail = ContactEmail,
            Website = Website,
            Domain = Domain.Trim(),
            LogoPath = null, // Will be set by controller after file save
            IsInternational = IsInternational,
            CurrentInvoiceNo = CurrentInvoiceNo,
            SendGridName = SendGridName,
            SuffixKeyName = SuffixKeyName,
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }
}

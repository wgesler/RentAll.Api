using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Organizations.Organizations;

public class UpdateOrganizationDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationCode { get; set; } = string.Empty;
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
    public string? LogoPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsInternational { get; set; }
    public int CurrentInvoiceNo { get; set; }
    public string? SendGridName { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (string.IsNullOrWhiteSpace(OrganizationCode))
            return (false, "OrganizationCode is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (OrganizationTypeId <= 0)
            return (false, "OrganizationTypeId is required");

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

    public Organization ToModel(Guid currentUser)
    {
        return new Organization
        {
            OrganizationId = OrganizationId,
            OrganizationCode = OrganizationCode,
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
            LogoPath = LogoPath,
            FileDetails = FileDetails,
            IsInternational = IsInternational,
            CurrentInvoiceNo = CurrentInvoiceNo,
            SendGridName = SendGridName,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}

using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Companies;

public class UpdateCompanyDto
{
    public Guid OrganizationId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? LogoPath { get; set; }
    public RentAll.Domain.Models.Common.FileDetails? FileDetails { get; set; }
	public string? Notes { get; set; }
	public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
    {
        if (id == Guid.Empty)
            return (false, "Company ID is required");

        if (CompanyId != id)
            return (false, "Company ID mismatch");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (string.IsNullOrWhiteSpace(CompanyCode))
            return (false, "Company Code is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (string.IsNullOrWhiteSpace(Address1))
            return (false, "Address1 is required");

        if (string.IsNullOrWhiteSpace(City))
            return (false, "City is required");

        if (string.IsNullOrWhiteSpace(State))
            return (false, "State is required");

        if (string.IsNullOrWhiteSpace(Zip))
            return (false, "Zip is required");

        if (string.IsNullOrWhiteSpace(Phone))
            return (false, "Phone is required");

        return (true, null);
    }

    public Company ToModel(Guid currentUser)
    {
        return new Company
        {
            OrganizationId = OrganizationId,
            CompanyId = CompanyId,
            CompanyCode = CompanyCode,
            Name = Name,
            Address1 = Address1,
            Address2 = Address2,
            Suite = Suite,
            City = City,
            State = State,
            Zip = Zip,
            Phone = Phone,
            Website = Website,
            LogoPath = LogoPath, // Will be updated by controller if FileDetails provided
			Notes = Notes,
			IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}

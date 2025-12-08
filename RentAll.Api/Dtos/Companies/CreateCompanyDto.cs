using RentAll.Domain.Models.Companies;

namespace RentAll.Api.Dtos.Companies;

public class CreateCompanyDto
{
    public string CompanyCode { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public Guid? LogoStorageId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
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

    public Company ToModel(CreateCompanyDto c, Guid currentUser)
    {
        return new Company
        {
            CompanyCode = c.CompanyCode,
            ContactId = c.ContactId,
            Name = c.Name,
            Address1 = c.Address1,
            Address2 = c.Address2,
            City = c.City,
            State = c.State,
            Zip = c.Zip,
            Phone = c.Phone,
            Website = c.Website,
            LogoStorageId = c.LogoStorageId,
            IsActive = true,
            CreatedBy = currentUser
		};
    }
}

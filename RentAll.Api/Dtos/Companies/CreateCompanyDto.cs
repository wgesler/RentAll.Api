using RentAll.Domain.Models.Companies;

namespace RentAll.Api.Dtos.Companies;

public class CreateCompanyDto
{
	public string CompanyCode { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Address1 { get; set; } = string.Empty;
	public string? Address2 { get; set; }
	public string City { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
	public string Zip { get; set; } = string.Empty;
	public string Phone { get; set; } = string.Empty;
	public string? Website { get; set; }

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

	public Company ToModel(Guid currentUser)
	{
		return new Company
		{
			CompanyCode = CompanyCode,
			Name = Name,
			Address1 = Address1,
			Address2 = Address2,
			City = City,
			State = State,
			Zip = Zip,
			Phone = Phone,
			Website = Website,
			IsActive = 1,
			CreatedBy = currentUser
		};
	}
}


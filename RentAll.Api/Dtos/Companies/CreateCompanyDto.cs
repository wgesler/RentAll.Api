using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Companies;

public class CreateCompanyDto
{
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Address1 { get; set; } = string.Empty;
	public string? Address2 { get; set; }
	public string? Suite { get; set; }
	public string? City { get; set; }
	public string? State { get; set; }
	public string? Zip { get; set; }
	public string Phone { get; set; } = string.Empty;
	public string? Website { get; set; }
	public FileDetails? FileDetails { get; set; }
	public string? Notes { get; set; }
	public bool IsInternational { get; set; }
	public bool IsActive { get; set; }


	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (OfficeId <= 0)
			return (false, "OfficeId is required");

		if (string.IsNullOrWhiteSpace(Name))
			return (false, "Name is required");

		if (string.IsNullOrWhiteSpace(Address1))
			return (false, "Address1 is required");


		if (string.IsNullOrWhiteSpace(Phone))
			return (false, "Phone is required");

		return (true, null);
	}

	public Company ToModel(string code, Guid currentUser)
	{
		return new Company
		{
			OrganizationId = OrganizationId,
			OfficeId = OfficeId,
			CompanyCode = code,
			Name = Name,
			Address1 = Address1,
			Address2 = Address2,
			Suite = Suite,
			City = City,
			State = State,
			Zip = Zip,
			Phone = Phone,
			Website = Website,
			LogoPath = null, // Will be set by controller after file save
			Notes = Notes,
			IsInternational = IsInternational,
			IsActive = IsActive,
			CreatedBy = currentUser
		};
	}
}

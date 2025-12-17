using RentAll.Domain.Models.Organizations;

namespace RentAll.Api.Dtos.Organizations;

public class UpdateOrganizationDto
{
	public Guid OrganizationId { get; set; }
	public string OrganizationCode { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Address1 { get; set; } = string.Empty;
	public string? Address2 { get; set; }
	public string? Suite { get; set; }
	public string City { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
	public string Zip { get; set; } = string.Empty;
	public string Phone { get; set; } = string.Empty;
	public string? Website { get; set; }
	public Guid? LogoStorageId { get; set; }
	public bool IsActive { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
	{
		if (id == Guid.Empty)
			return (false, "OrganizationId is required");

		if (OrganizationId != id)
			return (false, "OrganizationId mismatch");

		if (string.IsNullOrWhiteSpace(OrganizationCode))
			return (false, "OrganizationCode is required");

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

	public Organization ToModel(UpdateOrganizationDto d, Guid currentUser)
	{
		return new Organization
		{
			OrganizationId = d.OrganizationId,
			OrganizationCode = d.OrganizationCode,
			Name = d.Name,
			Address1 = d.Address1,
			Address2 = d.Address2,
			Suite = d.Suite,
			City = d.City,
			State = d.State,
			Zip = d.Zip,
			Phone = d.Phone,
			Website = d.Website,
			LogoStorageId = d.LogoStorageId,
			IsActive = d.IsActive,
			ModifiedBy = currentUser
		};
	}
}



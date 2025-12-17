using RentAll.Domain.Models.Organizations;

namespace RentAll.Api.Dtos.Organizations;

public class CreateOrganizationDto
{
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

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
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

	public Organization ToModel(CreateOrganizationDto dto, string code, Guid currentUser)
	{
		return new Organization
		{
			OrganizationCode = code,
			Name = dto.Name,
			Address1 = dto.Address1,
			Address2 = dto.Address2,
			Suite = dto.Suite,
			City = dto.City,
			State = dto.State,
			Zip = dto.Zip,
			Phone = dto.Phone,
			Website = dto.Website,
			LogoStorageId = dto.LogoStorageId,
			IsActive = dto.IsActive,
			CreatedBy = currentUser
		};
	}
}



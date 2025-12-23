using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.PropertyWelcomes;

public class CreatePropertyWelcomeDto
{
	public Guid PropertyId { get; set; }
	public Guid OrganizationId { get; set; }
	public string WelcomeLetter { get; set; } = string.Empty;

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (PropertyId == Guid.Empty)
			return (false, "PropertyId is required");

		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (string.IsNullOrWhiteSpace(WelcomeLetter))
			return (false, "WelcomeLetter is required");

		return (true, null);
	}

	public PropertyWelcome ToModel(Guid currentUser)
	{
		return new PropertyWelcome
		{
			PropertyId = PropertyId,
			OrganizationId = OrganizationId,
			WelcomeLetter = WelcomeLetter,
			CreatedBy = currentUser
		};
	}
}



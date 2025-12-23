using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.PropertyWelcomes;

public class UpdatePropertyWelcomeDto
{
	public Guid PropertyId { get; set; }
	public Guid? OrganizationId { get; set; }
	public string WelcomeLetter { get; set; } = string.Empty;

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (PropertyId == Guid.Empty)
			return (false, "PropertyId is required");

		if (string.IsNullOrWhiteSpace(WelcomeLetter))
			return (false, "WelcomeLetter is required");

		return (true, null);
	}

	public PropertyWelcome ToModel(Guid currentUser, Guid currentOrganization)
	{
		return new PropertyWelcome
		{
			PropertyId = PropertyId,
			OrganizationId = currentOrganization,
			WelcomeLetter = WelcomeLetter,
			ModifiedBy = currentUser
		};
	}
}



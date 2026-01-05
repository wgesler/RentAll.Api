using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.PropertyHtmls;

public class UpdatePropertyHtmlDto
{
	public Guid PropertyId { get; set; }
	public Guid? OrganizationId { get; set; }
	public string WelcomeLetter { get; set; } = string.Empty;
	public string DefaultLease { get; set; } = string.Empty;

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (PropertyId == Guid.Empty)
			return (false, "PropertyId is required");

		if (string.IsNullOrWhiteSpace(WelcomeLetter))
			return (false, "WelcomeLetter is required");

		if (string.IsNullOrWhiteSpace(DefaultLease))
			return (false, "DefaultLease is required");

		return (true, null);
	}

	public PropertyHtml ToModel(Guid currentUser, Guid currentOrganization)
	{
		return new PropertyHtml
		{
			PropertyId = PropertyId,
			OrganizationId = currentOrganization,
			WelcomeLetter = WelcomeLetter,
			DefaultLease = DefaultLease,
			ModifiedBy = currentUser
		};
	}
}


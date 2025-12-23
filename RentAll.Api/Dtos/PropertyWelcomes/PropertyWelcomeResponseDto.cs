using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.PropertyWelcomes;

public class PropertyWelcomeResponseDto
{
	public Guid PropertyId { get; set; }
	public Guid OrganizationId { get; set; }
	public string WelcomeLetter { get; set; } = string.Empty;

	public PropertyWelcomeResponseDto(PropertyWelcome propertyWelcome)
	{
		PropertyId = propertyWelcome.PropertyId;
		OrganizationId = propertyWelcome.OrganizationId;
		WelcomeLetter = propertyWelcome.WelcomeLetter;
	}
}



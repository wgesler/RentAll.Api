using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Dtos.Buildings;

public class BuildingCreateDto
{
	public Guid OrganizationId { get; set; }
	public string BuildingCode { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public Building ToModel()
	{
		return new Building
		{
			OrganizationId = OrganizationId,
			BuildingCode = BuildingCode,
			Description = Description,
			IsActive = IsActive
		};
	}
}


using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Dtos.Regions;

public class RegionUpdateDto
{
	public Guid OrganizationId { get; set; }
	public int RegionId { get; set; }
	public string RegionCode { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public Region ToModel()
	{
		return new Region
		{
			OrganizationId = OrganizationId,
			RegionId = RegionId,
			RegionCode = RegionCode,
			Description = Description,
			IsActive = IsActive
		};
	}
}


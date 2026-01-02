using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Regions;

public class RegionUpdateDto
{
	public Guid OrganizationId { get; set; }
	public int RegionId { get; set; }
	public int? OfficeId { get; set; }
	public string RegionCode { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public Region ToModel()
	{
		return new Region
		{
			OrganizationId = OrganizationId,
			RegionId = RegionId,
			OfficeId = OfficeId,
			RegionCode = RegionCode,
			Name = Name,
			Description = Description,
			IsActive = IsActive
		};
	}
}


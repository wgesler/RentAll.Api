using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Regions;

public class RegionCreateDto
{
	public Guid OrganizationId { get; set; }
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
			OfficeId = OfficeId,
			RegionCode = RegionCode,
			Name = Name,
			Description = Description,
			IsActive = IsActive
		};
	}
}


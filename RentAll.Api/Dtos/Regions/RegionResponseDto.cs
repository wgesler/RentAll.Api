using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Regions;

public class RegionResponseDto
{
	public Guid OrganizationId { get; set; }
	public int RegionId { get; set; }
	public string RegionCode { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public RegionResponseDto(Region region)
	{
		OrganizationId = region.OrganizationId;
		RegionId = region.RegionId;
		RegionCode = region.RegionCode;
		Description = region.Description;
		IsActive = region.IsActive;
	}
}


using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Dtos.Areas;

public class AreaResponseDto
{
	public Guid OrganizationId { get; set; }
	public int AreaId { get; set; }
	public string AreaCode { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public AreaResponseDto(Area area)
	{
		OrganizationId = area.OrganizationId;
		AreaId = area.AreaId;
		AreaCode = area.AreaCode;
		Description = area.Description;
		IsActive = area.IsActive;
	}
}
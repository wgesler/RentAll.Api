using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Areas;

public class AreaResponseDto
{
	public Guid OrganizationId { get; set; }
	public int AreaId { get; set; }
	public int? OfficeId { get; set; }
	public string OfficeName { get; set; } = string.Empty;
	public string AreaCode { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public AreaResponseDto(Area area)
	{
		OrganizationId = area.OrganizationId;
		AreaId = area.AreaId;
		OfficeId = area.OfficeId;
		OfficeName = area.OfficeName;
		AreaCode = area.AreaCode;
		Name = area.Name;
		Description = area.Description;
		IsActive = area.IsActive;
	}
}
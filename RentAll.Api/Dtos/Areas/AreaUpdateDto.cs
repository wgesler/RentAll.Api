using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Dtos.Areas;

public class AreaUpdateDto
{
	public Guid OrganizationId { get; set; }
	public int AreaId { get; set; }
	public string AreaCode { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public Area ToModel()
	{
		return new Area
		{
			OrganizationId = OrganizationId,
			AreaId = AreaId,
			AreaCode = AreaCode,
			Description = Description,
			IsActive = IsActive
		};
	}
}

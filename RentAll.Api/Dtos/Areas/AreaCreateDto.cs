using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Areas;

public class AreaCreateDto
{
	public Guid OrganizationId { get; set; }
	public string AreaCode { get; set; } = string.Empty;	
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public Area ToModel()
	{
		return new Area
		{
			OrganizationId = OrganizationId,
			AreaCode = AreaCode,
			Description = Description,
			IsActive = IsActive
		};
	}
}

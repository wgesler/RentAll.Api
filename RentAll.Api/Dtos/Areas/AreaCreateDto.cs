using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Areas;

public class AreaCreateDto
{
	public Guid OrganizationId { get; set; }
	public int? OfficeId { get; set; }
	public string AreaCode { get; set; } = string.Empty;	
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public Area ToModel()
	{
		return new Area
		{
			OrganizationId = OrganizationId,
			OfficeId = OfficeId,
			AreaCode = AreaCode,
			Name = Name,
			Description = Description,
			IsActive = IsActive
		};
	}
}

using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Areas;

public class AreaCreateDto
{
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string AreaCode { get; set; } = string.Empty;	
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (string.IsNullOrWhiteSpace(AreaCode))
			return (false, "Area Code is required");

		if (string.IsNullOrWhiteSpace(Name))
			return (false, "Name is required");

		if (string.IsNullOrWhiteSpace(Description))
			return (false, "Description is required");

		return (true, null);
	}

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

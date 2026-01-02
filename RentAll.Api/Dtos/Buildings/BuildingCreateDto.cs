using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Buildings;

public class BuildingCreateDto
{
	public Guid OrganizationId { get; set; }
	public int? OfficeId { get; set; }
	public string BuildingCode { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string? HoaName { get; set; }
	public string? HoaPhone { get; set; }
	public string? HoaEmail { get; set; }
	public bool IsActive { get; set; }

	public Building ToModel()
	{
		return new Building
		{
			OrganizationId = OrganizationId,
			OfficeId = OfficeId,
			BuildingCode = BuildingCode,
			Name = Name,
			Description = Description,
			HoaName = HoaName,
			HoaPhone = HoaPhone,
			HoaEmail = HoaEmail,
			IsActive = IsActive
		};
	}
}


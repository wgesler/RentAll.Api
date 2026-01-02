namespace RentAll.Infrastructure.Entities;

public class BuildingEntity
{
	public int BuildingId { get; set; }
	public Guid OrganizationId { get; set; }
	public int? OfficeId { get; set; }
	public string BuildingCode { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string? HoaName { get; set; }
	public string? HoaPhone { get; set; }
	public string? HoaEmail { get; set; }
	public bool IsActive { get; set; }
}


namespace RentAll.Domain.Models;

public class Region
{
	public int RegionId { get; set; }
	public Guid OrganizationId { get; set; }
	public int? OfficeId { get; set; }
	public string RegionCode { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }
}


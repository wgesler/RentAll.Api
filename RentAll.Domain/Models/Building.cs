namespace RentAll.Domain.Models;

public class Building
{
	public int BuildingId { get; set; }
	public Guid OrganizationId { get; set; }
	public string BuildingCode { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }
}


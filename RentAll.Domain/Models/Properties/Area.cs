namespace RentAll.Domain.Models.Properties;

public class Area
{
	public int AreaId { get; set; }
	public Guid OrganizationId { get; set; }
	public string AreaCode { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; }
}


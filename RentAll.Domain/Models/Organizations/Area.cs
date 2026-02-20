namespace RentAll.Domain.Models;

public class Area
{
    public int AreaId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string AreaCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}


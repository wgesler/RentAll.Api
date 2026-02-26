namespace RentAll.Api.Dtos.Organizations.Regions;

public class RegionResponseDto
{
    public Guid OrganizationId { get; set; }
    public int RegionId { get; set; }
    public int? OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public RegionResponseDto(Region region)
    {
        OrganizationId = region.OrganizationId;
        RegionId = region.RegionId;
        OfficeId = region.OfficeId;
        OfficeName = region.OfficeName;
        RegionCode = region.RegionCode;
        Name = region.Name;
        Description = region.Description;
        IsActive = region.IsActive;
    }
}


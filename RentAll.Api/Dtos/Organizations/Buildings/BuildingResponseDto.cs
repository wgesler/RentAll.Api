namespace RentAll.Api.Dtos.Organizations.Buildings;

public class BuildingResponseDto
{
    public Guid OrganizationId { get; set; }
    public int BuildingId { get; set; }
    public int? OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string BuildingCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HoaName { get; set; }
    public string? HoaPhone { get; set; }
    public string? HoaEmail { get; set; }
    public bool IsActive { get; set; }

    public BuildingResponseDto(Building building)
    {
        OrganizationId = building.OrganizationId;
        BuildingId = building.BuildingId;
        OfficeId = building.OfficeId;
        OfficeName = building.OfficeName;
        BuildingCode = building.BuildingCode;
        Name = building.Name;
        Description = building.Description;
        HoaName = building.HoaName;
        HoaPhone = building.HoaPhone;
        HoaEmail = building.HoaEmail;
        IsActive = building.IsActive;
    }
}


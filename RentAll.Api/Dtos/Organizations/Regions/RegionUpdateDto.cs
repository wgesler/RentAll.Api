using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Organizations.Regions;

public class RegionUpdateDto
{
    public Guid OrganizationId { get; set; }
    public int RegionId { get; set; }
    public int OfficeId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (RegionId <= 0)
            return (false, "Area ID is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "Office ID is required");

        if (string.IsNullOrWhiteSpace(RegionCode))
            return (false, "Area Code is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        return (true, null);
    }

    public Region ToModel()
    {
        return new Region
        {
            OrganizationId = OrganizationId,
            RegionId = RegionId,
            OfficeId = OfficeId,
            RegionCode = RegionCode,
            Name = Name,
            Description = Description,
            IsActive = IsActive
        };
    }
}


using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Organizations.Buildings;

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

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (string.IsNullOrWhiteSpace(BuildingCode))
            return (false, "Building Code is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        return (true, null);
    }
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


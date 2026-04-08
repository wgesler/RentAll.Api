namespace RentAll.Api.Dtos.Organizations.Buildings;

public class BuildingResponseDto
{
    public Guid OrganizationId { get; set; }
    public int BuildingId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string BuildingCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HoaName { get; set; }
    public string? HoaPhone { get; set; }
    public string? HoaEmail { get; set; }

    public bool Heating { get; set; }
    public bool Ac { get; set; }
    public bool Elevator { get; set; }
    public bool Security { get; set; }
    public bool Gated { get; set; }
    public bool PetsAllowed { get; set; }
    public bool DogsOkay { get; set; }
    public bool CatsOkay { get; set; }
    public string PoundLimit { get; set; } = string.Empty;
    public bool WasherDryerInBldg { get; set; }
    public bool Deck { get; set; }
    public bool Patio { get; set; }
    public bool Yard { get; set; }
    public bool Garden { get; set; }
    public bool CommonPool { get; set; }
    public bool PrivatePool { get; set; }
    public bool Jacuzzi { get; set; }
    public bool Sauna { get; set; }
    public bool Gym { get; set; }

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
        Heating = building.Heating;
        Ac = building.Ac;
        Elevator = building.Elevator;
        Security = building.Security;
        Gated = building.Gated;
        PetsAllowed = building.PetsAllowed;
        DogsOkay = building.DogsOkay;
        CatsOkay = building.CatsOkay;
        PoundLimit = building.PoundLimit;
        WasherDryerInBldg = building.WasherDryerInBldg;
        Deck = building.Deck;
        Patio = building.Patio;
        Yard = building.Yard;
        Garden = building.Garden;
        CommonPool = building.CommonPool;
        PrivatePool = building.PrivatePool;
        Jacuzzi = building.Jacuzzi;
        Sauna = building.Sauna;
        Gym = building.Gym;
        IsActive = building.IsActive;
    }
}


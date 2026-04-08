namespace RentAll.Api.Dtos.Organizations.Buildings;

public class BuildingCreateDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
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

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (string.IsNullOrWhiteSpace(BuildingCode))
            return (false, "Building Code is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

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
            Heating = Heating,
            Ac = Ac,
            Elevator = Elevator,
            Security = Security,
            Gated = Gated,
            PetsAllowed = PetsAllowed,
            DogsOkay = DogsOkay,
            CatsOkay = CatsOkay,
            PoundLimit = PoundLimit,
            WasherDryerInBldg = WasherDryerInBldg,
            Deck = Deck,
            Patio = Patio,
            Yard = Yard,
            Garden = Garden,
            CommonPool = CommonPool,
            PrivatePool = PrivatePool,
            Jacuzzi = Jacuzzi,
            Sauna = Sauna,
            Gym = Gym,
            IsActive = IsActive
        };
    }
}


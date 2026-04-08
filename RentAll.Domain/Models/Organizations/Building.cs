namespace RentAll.Domain.Models;

public class Building
{
    public int BuildingId { get; set; }
    public Guid OrganizationId { get; set; }
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
}


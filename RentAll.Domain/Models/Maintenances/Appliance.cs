namespace RentAll.Domain.Models;

public class Appliance
{
    public int ApplianceId { get; set; }
    public Guid PropertyId { get; set; }
    public string ApplianceName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string? ModelNo { get; set; }
    public string? SerialNo { get; set; }
    public string? DecalPath { get; set; }
}

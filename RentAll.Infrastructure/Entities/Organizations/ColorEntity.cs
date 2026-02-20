namespace RentAll.Infrastructure.Entities.Organizations;

public class ColorEntity
{
    public int ColorId { get; set; }
    public Guid OrganizationId { get; set; }
    public int ReservationStatusId { get; set; }
    public string Color { get; set; } = string.Empty;
}





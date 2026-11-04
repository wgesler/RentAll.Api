namespace RentAll.Domain.Models;

public class ReservationStatusSetting
{
    public Guid OrganizationId { get; set; }
    public int ReservationStatusId { get; set; }
    public bool IsEnabled { get; set; }
}

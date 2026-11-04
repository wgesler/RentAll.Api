namespace RentAll.Infrastructure.Entities.Organizations;

public class ReservationStatusSettingEntity
{
    public Guid OrganizationId { get; set; }
    public int ReservationStatusId { get; set; }
    public bool IsEnabled { get; set; }
}

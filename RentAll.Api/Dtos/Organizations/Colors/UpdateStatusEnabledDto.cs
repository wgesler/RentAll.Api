namespace RentAll.Api.Dtos.Organizations.Colors;

public class UpdateStatusEnabledDto
{
    public int ReservationStatusId { get; set; }
    public bool StatusEnabled { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (!Enum.IsDefined(typeof(ReservationStatus), ReservationStatusId))
            return (false, $"Invalid ReservationStatusId value: {ReservationStatusId}");

        return (true, null);
    }
}

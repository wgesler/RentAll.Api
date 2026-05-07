namespace RentAll.Api.Dtos.Reservations.Trackers;

public class ReservationTrackerResponseUpdateDto
{
    public Guid TrackerResponseId { get; set; }
    public Guid TrackerDefinitionId { get; set; }
    public Guid ReservationId { get; set; }
    public bool IsChecked { get; set; }
    public DateTimeOffset? CheckedOn { get; set; }
    public Guid? CheckedBy { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (TrackerResponseId == Guid.Empty)
            return (false, "TrackerResponseId is required");

        if (TrackerDefinitionId == Guid.Empty)
            return (false, "TrackerDefinitionId is required");

        if (ReservationId == Guid.Empty)
            return (false, "ReservationId is required");

        return (true, null);
    }
}


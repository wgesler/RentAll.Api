namespace RentAll.Api.Dtos.Reservations.Trackers;

public class ReservationTrackerResponseOptionUpdateDto
{
    public Guid TrackerResponseId { get; set; }
    public Guid TrackerDefinitionOptionId { get; set; }
    public Guid NewTrackerDefinitionOptionId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (TrackerResponseId == Guid.Empty)
            return (false, "TrackerResponseId is required");

        if (TrackerDefinitionOptionId == Guid.Empty)
            return (false, "TrackerDefinitionOptionId is required");

        if (NewTrackerDefinitionOptionId == Guid.Empty)
            return (false, "NewTrackerDefinitionOptionId is required");

        return (true, null);
    }
}


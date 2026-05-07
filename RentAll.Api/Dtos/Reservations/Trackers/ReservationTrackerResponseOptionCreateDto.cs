namespace RentAll.Api.Dtos.Reservations.Trackers;

public class ReservationTrackerResponseOptionCreateDto
{
    public Guid TrackerResponseId { get; set; }
    public Guid TrackerDefinitionOptionId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (TrackerResponseId == Guid.Empty)
            return (false, "TrackerResponseId is required");

        if (TrackerDefinitionOptionId == Guid.Empty)
            return (false, "TrackerDefinitionOptionId is required");

        return (true, null);
    }

    public TrackerResponseOption ToModel(Guid currentUser)
    {
        return new TrackerResponseOption
        {
            TrackerResponseId = TrackerResponseId,
            TrackerDefinitionOptionId = TrackerDefinitionOptionId,
            CreatedBy = currentUser
        };
    }
}


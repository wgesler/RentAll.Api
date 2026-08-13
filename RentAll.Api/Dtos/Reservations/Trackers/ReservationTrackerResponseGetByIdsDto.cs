namespace RentAll.Api.Dtos.Reservations.Trackers;

public class ReservationTrackerResponseGetByIdsDto
{
    public List<Guid> ReservationIds { get; set; } = new();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReservationIds == null)
            return (false, "ReservationIds is required");

        if (ReservationIds.Any(id => id == Guid.Empty))
            return (false, "ReservationIds cannot contain empty values");

        return (true, null);
    }
}

public class ReservationTrackerResponseGetByIdsResponseDto
{
    public List<ReservationTrackerResponseResponseDto> Responses { get; set; } = new();
    public List<ReservationTrackerResponseOptionResponseDto> Options { get; set; } = new();
}

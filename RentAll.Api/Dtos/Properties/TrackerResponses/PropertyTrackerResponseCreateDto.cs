namespace RentAll.Api.Dtos.Properties.TrackerResponses;

public class PropertyTrackerResponseCreateDto
{
    public Guid TrackerDefinitionId { get; set; }
    public Guid PropertyId { get; set; }
    public bool IsChecked { get; set; }
    public DateTimeOffset? CheckedOn { get; set; }
    public Guid? CheckedBy { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (TrackerDefinitionId == Guid.Empty)
            return (false, "TrackerDefinitionId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        return (true, null);
    }

    public TrackerResponse ToModel(Guid currentUser)
    {
        return new TrackerResponse
        {
            TrackerDefinitionId = TrackerDefinitionId,
            PropertyId = PropertyId,
            ReservationId = null,
            EntityTypeId = (int)EntityType.Property,
            EntityId = PropertyId,
            IsChecked = IsChecked,
            CheckedOn = CheckedOn,
            CheckedBy = CheckedBy,
            CreatedBy = currentUser
        };
    }
}


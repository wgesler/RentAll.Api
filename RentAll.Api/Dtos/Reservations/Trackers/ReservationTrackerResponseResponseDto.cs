namespace RentAll.Api.Dtos.Reservations.Trackers;

public class ReservationTrackerResponseResponseDto
{
    public Guid TrackerResponseId { get; set; }
    public Guid TrackerDefinitionId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public int TrackerContextId { get; set; }
    public string TrackerContextCode { get; set; } = string.Empty;
    public string TrackerDisplayName { get; set; } = string.Empty;
    public string? TrackerDescription { get; set; }
    public int TrackerSortOrder { get; set; }
    public int EntityTypeId { get; set; }
    public string EntityTypeDescription { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public bool IsChecked { get; set; }
    public DateTimeOffset? CheckedOn { get; set; }
    public Guid? CheckedBy { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public ReservationTrackerResponseResponseDto(TrackerResponse trackerResponse)
    {
        TrackerResponseId = trackerResponse.TrackerResponseId;
        TrackerDefinitionId = trackerResponse.TrackerDefinitionId;
        PropertyId = trackerResponse.PropertyId;
        ReservationId = trackerResponse.ReservationId;
        OrganizationId = trackerResponse.OrganizationId;
        OfficeId = trackerResponse.OfficeId;
        OfficeName = trackerResponse.OfficeName;
        TrackerContextId = (int)trackerResponse.TrackerContextId;
        TrackerContextCode = trackerResponse.TrackerContextCode;
        TrackerDisplayName = trackerResponse.TrackerDisplayName;
        TrackerDescription = trackerResponse.TrackerDescription;
        TrackerSortOrder = trackerResponse.TrackerSortOrder;
        EntityTypeId = trackerResponse.EntityTypeId;
        EntityTypeDescription = trackerResponse.EntityTypeDescription;
        EntityId = trackerResponse.EntityId;
        IsChecked = trackerResponse.IsChecked;
        CheckedOn = trackerResponse.CheckedOn;
        CheckedBy = trackerResponse.CheckedBy;
        CreatedOn = trackerResponse.CreatedOn;
        CreatedBy = trackerResponse.CreatedBy;
        ModifiedOn = trackerResponse.ModifiedOn;
        ModifiedBy = trackerResponse.ModifiedBy;
    }
}


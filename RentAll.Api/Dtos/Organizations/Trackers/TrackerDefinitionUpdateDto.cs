namespace RentAll.Api.Dtos.Organizations.Trackers;

public class TrackerDefinitionUpdateDto
{
    public Guid TrackerDefinitionId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int TrackerContextId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (TrackerDefinitionId == Guid.Empty)
            return (false, "TrackerDefinitionId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (TrackerContextId <= 0)
            return (false, "TrackerContextId is required");

        if (string.IsNullOrWhiteSpace(DisplayName))
            return (false, "DisplayName is required");

        if (SortOrder < 0)
            return (false, "SortOrder must be zero or greater");

        return (true, null);
    }

    public TrackerDefinition ToModel(Guid currentUser)
    {
        return new TrackerDefinition
        {
            TrackerDefinitionId = TrackerDefinitionId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            TrackerContextId = (TrackerContextType)TrackerContextId,
            DisplayName = DisplayName,
            Description = Description,
            SortOrder = SortOrder,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}


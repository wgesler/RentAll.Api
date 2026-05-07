namespace RentAll.Api.Dtos.Organizations.Trackers;

public class TrackerDefinitionOptionUpdateDto
{
    public Guid TrackerDefinitionOptionId { get; set; }
    public Guid TrackerDefinitionId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (TrackerDefinitionOptionId == Guid.Empty)
            return (false, "TrackerDefinitionOptionId is required");

        if (TrackerDefinitionId == Guid.Empty)
            return (false, "TrackerDefinitionId is required");

        if (string.IsNullOrWhiteSpace(Label))
            return (false, "Label is required");

        if (SortOrder < 0)
            return (false, "SortOrder must be zero or greater");

        return (true, null);
    }

    public TrackerDefinitionOption ToModel()
    {
        return new TrackerDefinitionOption
        {
            TrackerDefinitionOptionId = TrackerDefinitionOptionId,
            TrackerDefinitionId = TrackerDefinitionId,
            Label = Label,
            OptionDescription = Description,
            OptionSortOrder = SortOrder,
            IsActive = IsActive
        };
    }
}


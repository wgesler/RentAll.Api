namespace RentAll.Api.Dtos.Organizations.Trackers;

public class TrackerDefinitionOptionCreateDto
{
    public Guid TrackerDefinitionId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
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
            TrackerDefinitionId = TrackerDefinitionId,
            Label = Label,
            OptionDescription = Description,
            OptionSortOrder = SortOrder,
            IsActive = IsActive
        };
    }
}


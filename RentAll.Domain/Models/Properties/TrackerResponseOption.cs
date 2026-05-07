using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class TrackerResponseOption
{
    public Guid TrackerResponseId { get; set; }
    public Guid TrackerDefinitionOptionId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid TrackerDefinitionId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public TrackerContextType TrackerContextId { get; set; }
    public string TrackerContextCode { get; set; } = string.Empty;
    public string TrackerDisplayName { get; set; } = string.Empty;
    public string? TrackerDescription { get; set; }
    public int TrackerSortOrder { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? OptionDescription { get; set; }
    public int OptionSortOrder { get; set; }
    public int EntityTypeId { get; set; }
    public string EntityTypeDescription { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public bool IsChecked { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
}


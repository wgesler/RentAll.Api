namespace RentAll.Infrastructure.Entities.Properties;

public class PropertyPhotoImportItemEntity
{
    public int ImportItemId { get; set; }
    public Guid ImportId { get; set; }
    public int ItemIndex { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public byte Status { get; set; }
    public string? ErrorMessage { get; set; }
    public int? PhotoId { get; set; }
    public DateTimeOffset? StartedOn { get; set; }
    public DateTimeOffset? CompletedOn { get; set; }
}
